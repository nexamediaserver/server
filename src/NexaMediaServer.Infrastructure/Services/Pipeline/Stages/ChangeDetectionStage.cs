// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Marks work items as unchanged when the file already exists in the library
/// with matching size and modification time, allowing them to be skipped.
/// </summary>
public sealed class ChangeDetectionStage : IScanPipelineStage<ScanWorkItem, ScanWorkItem>
{
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;

    // Use a single combined cache to reduce memory overhead.
    // The FileStats struct is small (16 bytes) and stored inline.
    private readonly Dictionary<int, Dictionary<string, FileStats>> dataCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDetectionStage"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating database contexts.</param>
    public ChangeDetectionStage(IDbContextFactory<MediaServerContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public string Name => "change_detection";

    /// <summary>
    /// Gets the cached set of existing file paths for a library, or null if not yet loaded.
    /// Use this to avoid duplicate database queries when checking for orphaned files.
    /// </summary>
    /// <param name="libraryId">The library section identifier.</param>
    /// <returns>The cached path set, or null if not loaded.</returns>
    public HashSet<string>? GetCachedPaths(int libraryId)
    {
        if (!this.dataCache.TryGetValue(libraryId, out var cache))
        {
            return null;
        }

        // Return the keys as a HashSet for compatibility with existing deletion logic
        return new HashSet<string>(cache.Keys, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clears the cached paths and stats for a specific library to free memory.
    /// Call this after a scan completes to prevent unbounded cache growth.
    /// </summary>
    /// <param name="libraryId">The library section identifier.</param>
    public void ClearCache(int libraryId)
    {
        if (this.dataCache.Remove(libraryId, out var cache))
        {
            cache.Clear(); // Help GC by clearing the dictionary
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<ScanWorkItem> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var libraryId = context.LibrarySection.Id;
        var existingData = await this.LoadExistingFileDataAsync(libraryId, cancellationToken);

        await foreach (var item in input.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip directories - they don't have change detection
            if (item.File.IsDirectory)
            {
                yield return item;
                continue;
            }

            var path = item.File.Path;

            // Check if path exists in database and get stats in one lookup
            if (!existingData.TryGetValue(path, out var stats))
            {
                // New file - needs processing
                yield return item;
                continue;
            }

            // Check if file has changed based on size and modification time
            var isUnchanged = IsFileUnchanged(item.File, stats);
            yield return item with
            {
                IsUnchanged = isUnchanged,
            };
        }
    }

    private static bool IsFileUnchanged(FileSystemMetadata file, FileStats existingStats)
    {
        // If we don't have current file stats, assume changed
        if (!file.Size.HasValue || !file.LastModifiedTimeUtc.HasValue)
        {
            return false;
        }

        // If we don't have stored stats, assume changed
        if (!existingStats.Size.HasValue || !existingStats.ModifiedAt.HasValue)
        {
            return false;
        }

        // Compare size first (fast check)
        if (file.Size.Value != existingStats.Size.Value)
        {
            return false;
        }

        // Compare modification time with tolerance for filesystem timestamp precision
        // (some filesystems have 2-second resolution)
        var timeDiff = Math.Abs(
            (file.LastModifiedTimeUtc.Value - existingStats.ModifiedAt.Value).TotalSeconds
        );
        return timeDiff < 2.0;
    }

    private async Task<Dictionary<string, FileStats>> LoadExistingFileDataAsync(
        int libraryId,
        CancellationToken cancellationToken
    )
    {
        // Return cached data if available
        if (this.dataCache.TryGetValue(libraryId, out var cachedData))
        {
            return cachedData;
        }

        // Load paths and stats from database using streaming to reduce peak memory
        await using var context = await this
            .dbContextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        // Pre-size dictionary based on estimated count to reduce reallocations
        var estimatedCount = await context
            .MediaParts.AsNoTracking()
            .Join(context.MediaItems, mp => mp.MediaItemId, mi => mi.Id, (mp, mi) => new { mp, mi })
            .Join(
                context.MetadataItems,
                x => x.mi.MetadataItemId,
                m => m.Id,
                (x, m) => new { x.mp, m.LibrarySectionId }
            )
            .Where(x => x.LibrarySectionId == libraryId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var data = new Dictionary<string, FileStats>(estimatedCount, StringComparer.OrdinalIgnoreCase);

        // Stream results to avoid loading all data into an intermediate list
        await foreach (
            var item in context
                .MediaParts.AsNoTracking()
                .Join(context.MediaItems, mp => mp.MediaItemId, mi => mi.Id, (mp, mi) => new { mp, mi })
                .Join(
                    context.MetadataItems,
                    x => x.mi.MetadataItemId,
                    m => m.Id,
                    (x, m) => new { x.mp, m.LibrarySectionId }
                )
                .Where(x => x.LibrarySectionId == libraryId)
                .Select(x => new { x.mp.File, x.mp.Size, x.mp.ModifiedAt })
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            data.TryAdd(item.File, new FileStats(item.Size, item.ModifiedAt));
        }

        this.dataCache[libraryId] = data;
        return data;
    }

    private readonly record struct FileStats(long? Size, DateTime? ModifiedAt);
}
