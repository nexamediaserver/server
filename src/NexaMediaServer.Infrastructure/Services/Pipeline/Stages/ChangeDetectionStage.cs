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
    private readonly Dictionary<int, HashSet<string>> pathCache = new();
    private readonly Dictionary<int, Dictionary<string, FileStats>> statsCache = new();

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
    public HashSet<string>? GetCachedPaths(int libraryId) =>
        this.pathCache.TryGetValue(libraryId, out var paths) ? paths : null;

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<ScanWorkItem> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var libraryId = context.LibrarySection.Id;
        var (existingPaths, existingStats) = await this.LoadExistingFileDataAsync(
            libraryId,
            cancellationToken
        );

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

            // Check if path exists in database
            if (!existingPaths.Contains(path))
            {
                // New file - needs processing
                yield return item;
                continue;
            }

            // Check if file has changed based on size and modification time
            if (existingStats.TryGetValue(path, out var stats))
            {
                var isUnchanged = IsFileUnchanged(item.File, stats);
                yield return item with
                {
                    IsUnchanged = isUnchanged,
                };
            }
            else
            {
                // Path exists but no stats - treat as potentially changed
                yield return item;
            }
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

    private async Task<(
        HashSet<string> Paths,
        Dictionary<string, FileStats> Stats
    )> LoadExistingFileDataAsync(int libraryId, CancellationToken cancellationToken)
    {
        // Return cached data if available
        if (
            this.pathCache.TryGetValue(libraryId, out var cachedPaths)
            && this.statsCache.TryGetValue(libraryId, out var cachedStats)
        )
        {
            return (cachedPaths, cachedStats);
        }

        // Load paths and stats from database
        await using var context = await this
            .dbContextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var fileData = await context
            .MediaParts.AsNoTracking()
            .Join(context.MediaItems, mp => mp.MediaItemId, mi => mi.Id, (mp, mi) => new { mp, mi })
            .Join(
                context.MetadataItems,
                x => x.mi.MetadataItemId,
                m => m.Id,
                (x, m) => new { x.mp, m.LibrarySectionId }
            )
            .Where(x => x.LibrarySectionId == libraryId)
            .Select(x => new
            {
                x.mp.File,
                x.mp.Size,
                x.mp.ModifiedAt,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var paths = fileData.Select(f => f.File).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stats = fileData.ToDictionary(
            f => f.File,
            f => new FileStats(f.Size, f.ModifiedAt),
            StringComparer.OrdinalIgnoreCase
        );

        this.pathCache[libraryId] = paths;
        this.statsCache[libraryId] = stats;

        return (paths, stats);
    }

    private readonly record struct FileStats(long? Size, DateTime? ModifiedAt);
}
