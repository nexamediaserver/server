// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Implementation of <see cref="IMetadataDeduplicationService"/> that uses external identifiers
/// to find and deduplicate metadata items.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains an in-memory cache of items created or found during a scan batch
/// to avoid repeated database queries. The cache is keyed by a composite of metadata type,
/// provider, and external ID.
/// </para>
/// <para>
/// Thread-safe for concurrent access during parallel scanning operations.
/// </para>
/// </remarks>
public sealed partial class MetadataDeduplicationService : IMetadataDeduplicationService
{
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<MetadataDeduplicationService> logger;

    // Cache key format: "{metadataType}:{provider}:{externalId}"
    private readonly ConcurrentDictionary<string, MetadataItem> cache = new(StringComparer.OrdinalIgnoreCase);

    // Track pending external IDs to register (item ID → list of provider:id pairs)
    private readonly ConcurrentDictionary<int, ConcurrentBag<(string Provider, string ExternalId)>> pendingRegistrations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataDeduplicationService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Database context factory.</param>
    /// <param name="logger">Logger instance.</param>
    public MetadataDeduplicationService(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ILogger<MetadataDeduplicationService> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<MetadataItem> FindOrCreateByExternalIdAsync(
        MetadataType metadataType,
        string provider,
        string externalId,
        int librarySectionId,
        Func<MetadataItem> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentNullException.ThrowIfNull(factory);

        var cacheKey = BuildCacheKey(metadataType, provider, externalId);

        // Check in-memory cache first
        if (this.cache.TryGetValue(cacheKey, out var cachedItem))
        {
            this.LogCacheHit(metadataType, provider, externalId);
            return cachedItem;
        }

        // Query database
        var existingItem = await this.FindByExternalIdAsync(
            metadataType,
            provider,
            externalId,
            librarySectionId,
            cancellationToken).ConfigureAwait(false);

        if (existingItem is not null)
        {
            // Add to cache and return
            this.cache.TryAdd(cacheKey, existingItem);
            this.LogDatabaseHit(metadataType, existingItem.Id, provider, externalId);
            return existingItem;
        }

        // Create new item via factory
        var newItem = factory();
        newItem.MetadataType = metadataType;
        newItem.LibrarySectionId = librarySectionId;

        // Track in cache (item may not have ID yet if not persisted)
        this.cache.TryAdd(cacheKey, newItem);

        // Queue the external ID registration for when the item is persisted
        this.QueueExternalIdRegistration(newItem, provider, externalId);

        this.LogCreatedNew(metadataType, provider, externalId);
        return newItem;
    }

    /// <inheritdoc />
    public async Task<MetadataItem> FindOrCreateByExternalIdsAsync(
        MetadataType metadataType,
        IReadOnlyDictionary<string, string> externalIds,
        int librarySectionId,
        Func<MetadataItem> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalIds);
        ArgumentNullException.ThrowIfNull(factory);

        if (externalIds.Count == 0)
        {
            return factory();
        }

        // Check cache for any matching ID
        foreach (var (provider, externalId) in externalIds)
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                continue;
            }

            var cacheKey = BuildCacheKey(metadataType, provider, externalId);
            if (this.cache.TryGetValue(cacheKey, out var cachedItem))
            {
                // Register any additional external IDs for this item
                foreach (var (p, id) in externalIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && p != provider)
                    {
                        this.QueueExternalIdRegistration(cachedItem, p, id);
                        this.cache.TryAdd(BuildCacheKey(metadataType, p, id), cachedItem);
                    }
                }

                return cachedItem;
            }
        }

        // Query database for any matching ID
        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var validIds = externalIds
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .ToList();

        if (validIds.Count == 0)
        {
            return factory();
        }

        // Build query for any matching external ID
        var query = context.MetadataItems
            .Where(m => m.MetadataType == metadataType && m.LibrarySectionId == librarySectionId)
            .Where(m => m.ExternalIdentifiers.Any(e =>
                validIds.Any(v => e.Provider == v.Key && e.Value == v.Value)));

        var existingItem = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (existingItem is not null)
        {
            // Add all IDs to cache pointing to this item
            foreach (var (provider, externalId) in validIds)
            {
                var cacheKey = BuildCacheKey(metadataType, provider, externalId);
                this.cache.TryAdd(cacheKey, existingItem);

                // Queue registration for IDs not yet in database
                this.QueueExternalIdRegistration(existingItem, provider, externalId);
            }

            return existingItem;
        }

        // Create new item via factory
        var newItem = factory();
        newItem.MetadataType = metadataType;
        newItem.LibrarySectionId = librarySectionId;

        // Add all IDs to cache
        foreach (var (provider, externalId) in validIds)
        {
            var cacheKey = BuildCacheKey(metadataType, provider, externalId);
            this.cache.TryAdd(cacheKey, newItem);
            this.QueueExternalIdRegistration(newItem, provider, externalId);
        }

        return newItem;
    }

    /// <inheritdoc />
    public async Task<MetadataItem?> FindByExternalIdAsync(
        MetadataType metadataType,
        string provider,
        string externalId,
        int? librarySectionId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var query = context.MetadataItems
            .Where(m => m.MetadataType == metadataType)
            .Where(m => m.ExternalIdentifiers.Any(e => e.Provider == provider && e.Value == externalId));

        if (librarySectionId.HasValue)
        {
            query = query.Where(m => m.LibrarySectionId == librarySectionId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RegisterExternalIdAsync(
        MetadataItem metadataItem,
        string provider,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadataItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        // Check if already registered
        if (metadataItem.ExternalIdentifiers.Any(e =>
            e.Provider == provider && e.Value == externalId))
        {
            return;
        }

        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Check database for existing registration
        var exists = await context.ExternalIdentifiers
            .AnyAsync(e =>
                e.MetadataItemId == metadataItem.Id &&
                e.Provider == provider &&
                e.Value == externalId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            context.ExternalIdentifiers.Add(new ExternalIdentifier
            {
                MetadataItemId = metadataItem.Id,
                Provider = provider,
                Value = externalId,
            });

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this.LogRegisteredExternalId(metadataItem.Id, provider, externalId);
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        var cacheCount = this.cache.Count;
        var pendingCount = this.pendingRegistrations.Count;

        this.cache.Clear();
        this.pendingRegistrations.Clear();

        this.LogCacheCleared(cacheCount, pendingCount);
    }

    /// <summary>
    /// Gets pending external ID registrations for a metadata item.
    /// </summary>
    /// <param name="itemId">The metadata item ID.</param>
    /// <returns>Collection of provider/ID pairs pending registration.</returns>
    public IEnumerable<(string Provider, string ExternalId)> GetPendingRegistrations(int itemId)
    {
        if (this.pendingRegistrations.TryGetValue(itemId, out var registrations))
        {
            return registrations.ToArray();
        }

        return [];
    }

    /// <summary>
    /// Clears pending registrations for a specific item after they have been processed.
    /// </summary>
    /// <param name="itemId">The metadata item ID.</param>
    public void ClearPendingRegistrations(int itemId)
    {
        this.pendingRegistrations.TryRemove(itemId, out _);
    }

    private static string BuildCacheKey(MetadataType metadataType, string provider, string externalId)
    {
        return $"{(int)metadataType}:{provider}:{externalId}";
    }

    private void QueueExternalIdRegistration(MetadataItem item, string provider, string externalId)
    {
        // For items not yet persisted (Id == 0), we track by object reference
        // For persisted items, we track by Id
        var key = item.Id > 0 ? item.Id : item.GetHashCode();

        var registrations = this.pendingRegistrations.GetOrAdd(key, _ => []);
        registrations.Add((provider, externalId));
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deduplication cache hit for {MetadataType} with {Provider}={ExternalId}")]
    private partial void LogCacheHit(MetadataType metadataType, string provider, string externalId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found existing {MetadataType} (Id={ItemId}) by {Provider}={ExternalId}")]
    private partial void LogDatabaseHit(MetadataType metadataType, int itemId, string provider, string externalId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created new {MetadataType} for {Provider}={ExternalId}")]
    private partial void LogCreatedNew(MetadataType metadataType, string provider, string externalId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Registered external ID {Provider}={ExternalId} for item {ItemId}")]
    private partial void LogRegisteredExternalId(int itemId, string provider, string externalId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Cleared deduplication cache ({CacheCount} items, {PendingCount} pending registrations)")]
    private partial void LogCacheCleared(int cacheCount, int pendingCount);
}
