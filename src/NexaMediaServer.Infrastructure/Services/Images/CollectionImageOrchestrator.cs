// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Service for orchestrating collection image generation.
/// </summary>
/// <remarks>
/// This service is responsible for generating collage thumbnails for collection-type metadata items
/// (e.g., PhotoAlbum, PictureSet, BookSeries, GameSeries, Collection, Playlist).
/// It runs as a separate process after all metadata refreshes for a scan are complete.
/// </remarks>
public sealed partial class CollectionImageOrchestrator : ICollectionImageOrchestrator
{
    /// <summary>
    /// Batch size for processing collections to avoid memory pressure.
    /// </summary>
    private const int BatchSize = 50;

    /// <summary>
    /// Maximum degree of parallelism for processing collections within a batch.
    /// </summary>
    private const int MaxParallelism = 4;

    /// <summary>
    /// Collection MetadataTypes that should have collage thumbnails generated.
    /// </summary>
    private static readonly FrozenSet<MetadataType> CollectionTypes = new HashSet<MetadataType>
    {
        MetadataType.PhotoAlbum,
        MetadataType.PictureSet,
        MetadataType.BookSeries,
        MetadataType.GameFranchise,
        MetadataType.GameSeries,
        MetadataType.Collection,
        MetadataType.Playlist,
    }.ToFrozenSet();

    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<CollectionImageOrchestrator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionImageOrchestrator"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating database contexts.</param>
    /// <param name="serviceScopeFactory">Factory for creating service scopes.</param>
    /// <param name="logger">The logger instance.</param>
    public CollectionImageOrchestrator(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CollectionImageOrchestrator> logger
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    [Queue("image_generators")]
    [AutomaticRetry(Attempts = 2)]
    public async Task GenerateCollectionImagesForLibraryAsync(
        int libraryId,
        CancellationToken cancellationToken = default
    )
    {
        this.LogGenerationStarted(libraryId);

        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Query all collection-type items in the library that have at least one child with a thumbnail
        var collectionUuids = await context.MetadataItems
            .AsNoTracking()
            .Where(m =>
                m.LibrarySectionId == libraryId
                && CollectionTypes.Contains(m.MetadataType)
                && m.Children.Any(c => c.ThumbUri != null))
            .Select(m => m.Uuid)
            .ToListAsync(cancellationToken);

        this.LogFoundCollections(libraryId, collectionUuids.Count);

        if (collectionUuids.Count == 0)
        {
            this.LogGenerationCompleted(libraryId, 0);
            return;
        }

        var processedCount = 0;

        // Process in batches to avoid memory pressure
        foreach (var batch in collectionUuids.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = cancellationToken,
            };

            await Parallel.ForEachAsync(batch, parallelOptions, async (uuid, ct) =>
            {
                try
                {
                    await this.GenerateCollectionImageInternalAsync(uuid, ct);
                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    this.LogCollectionImageError(uuid, ex);
                }
            });
        }

        this.LogGenerationCompleted(libraryId, processedCount);
    }

    /// <inheritdoc />
    [Queue("image_generators")]
    [AutomaticRetry(Attempts = 1)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RegenerateCollectionImageAsync(
        Guid parentUuid,
        CancellationToken cancellationToken = default
    )
    {
        this.LogRegenerationStarted(parentUuid);

        try
        {
            await this.GenerateCollectionImageInternalAsync(parentUuid, cancellationToken);
            this.LogRegenerationCompleted(parentUuid);
        }
        catch (Exception ex)
        {
            this.LogCollectionImageError(parentUuid, ex);
            throw;
        }
    }

    private async Task GenerateCollectionImageInternalAsync(
        Guid collectionUuid,
        CancellationToken cancellationToken
    )
    {
        using var scope = this.serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMetadataItemRepository>();
        var imageProvider = scope.ServiceProvider.GetRequiredService<CollectionImageProvider>();

        // Load the collection with its first media item (collections typically have one virtual MediaItem)
        var collection = await repository
            .GetQueryable()
            .Include(m => m.MediaItems)
            .FirstOrDefaultAsync(m => m.Uuid == collectionUuid, cancellationToken);

        if (collection == null)
        {
            this.LogCollectionNotFound(collectionUuid);
            return;
        }

        if (!CollectionTypes.Contains(collection.MetadataType))
        {
            this.LogNotACollectionType(collectionUuid, collection.MetadataType);
            return;
        }

        // Map to DTO
        var dto = MetadataItemMapper.Map(collection);
        if (dto is not MetadataCollectionItem collectionDto)
        {
            this.LogNotACollectionType(collectionUuid, collection.MetadataType);
            return;
        }

        // Get or create a virtual MediaItem for the collection
        var mediaItem = collection.MediaItems?.FirstOrDefault() ?? new MediaItem
        {
            Id = 0,
            MetadataItem = collection,
            MetadataItemId = collection.Id,
        };

        // Run the image provider
        await imageProvider.ProvideAsync(
            mediaItem,
            collectionDto,
            Array.Empty<MediaPart>(),
            cancellationToken
        );

        // Save the updated thumbnail URI if it changed
        if (collectionDto.ThumbUri != null && collectionDto.ThumbUri != collection.ThumbUri)
        {
            collection.ThumbUri = collectionDto.ThumbUri;
            await repository.UpdateAsync(collection);
            this.LogCollectionImageSaved(collectionUuid, collectionDto.ThumbUri);
        }
    }

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting collection image generation for library {LibraryId}"
    )]
    private partial void LogGenerationStarted(int libraryId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {Count} collections with children in library {LibraryId}"
    )]
    private partial void LogFoundCollections(int libraryId, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Completed collection image generation for library {LibraryId}: {ProcessedCount} collections processed"
    )]
    private partial void LogGenerationCompleted(int libraryId, int processedCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting regeneration of collection image for {CollectionUuid}"
    )]
    private partial void LogRegenerationStarted(Guid collectionUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed regeneration of collection image for {CollectionUuid}"
    )]
    private partial void LogRegenerationCompleted(Guid collectionUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Collection {CollectionUuid} not found"
    )]
    private partial void LogCollectionNotFound(Guid collectionUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Item {CollectionUuid} is not a collection type: {MetadataType}"
    )]
    private partial void LogNotACollectionType(Guid collectionUuid, MetadataType metadataType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved collection image for {CollectionUuid}: {ThumbUri}"
    )]
    private partial void LogCollectionImageSaved(Guid collectionUuid, string thumbUri);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error generating collection image for {CollectionUuid}"
    )]
    private partial void LogCollectionImageError(Guid collectionUuid, Exception ex);

    #endregion
}
