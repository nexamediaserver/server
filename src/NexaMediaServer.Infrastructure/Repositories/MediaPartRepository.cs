// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing media parts.
/// </summary>
public class MediaPartRepository : IMediaPartRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaPartRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public MediaPartRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<MediaPart> GetQueryable()
    {
        return this.context.MediaParts.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<MediaPart?> GetByIdAsync(int id)
    {
        return this.context.MediaParts.AsNoTracking().FirstOrDefaultAsync(mp => mp.Id == id);
    }

    /// <inheritdoc />
    public Task DeleteByFilePathAsync(string part)
    {
        return this.context.MediaParts.Where(mp => mp.File == part).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetFilePathsByLibraryIdAsync(
        int libraryId,
        CancellationToken cancellationToken = default
    )
    {
        // Join MediaParts -> MediaItems -> MetadataItems to filter by library section id.
        // Project only the File column, no tracking, and return a case-insensitive HashSet.
        var paths = await this
            .context.MediaParts.AsNoTracking()
            .Join(
                this.context.MediaItems,
                mp => mp.MediaItemId,
                mi => mi.Id,
                (mp, mi) => new { mp, mi }
            )
            .Join(
                this.context.MetadataItems,
                x => x.mi.MetadataItemId,
                m => m.Id,
                (x, m) => new { x.mp, m.LibrarySectionId }
            )
            .Where(x => x.LibrarySectionId == libraryId)
            .Select(x => x.mp.File)
            .Distinct()
            .ToListAsync(cancellationToken);

        return paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByFilePathsAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default
    )
    {
        // Execute in chunks to avoid hitting SQLite/SQL parameter limits.
        const int chunkSize = 500;
        var totalDeleted = 0;
        var pending = new List<string>(chunkSize);

        foreach (var path in filePaths ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            pending.Add(path);
            if (pending.Count >= chunkSize)
            {
                totalDeleted += await this
                    .context.MediaParts.Where(mp => pending.Contains(mp.File))
                    .ExecuteDeleteAsync(cancellationToken);
                pending.Clear();
            }
        }

        if (pending.Count > 0)
        {
            totalDeleted += await this
                .context.MediaParts.Where(mp => pending.Contains(mp.File))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return totalDeleted;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> StreamFilePathsByLibraryIdAsync(
        int libraryId,
        CancellationToken cancellationToken = default
    )
    {
        // Stream results: projection to File with filtered join, no tracking.
        // The caller should enumerate with await foreach.
        IAsyncEnumerable<string> query = this
            .context.MediaParts.AsNoTracking()
            .Join(
                this.context.MediaItems,
                mp => mp.MediaItemId,
                mi => mi.Id,
                (mp, mi) => new { mp, mi }
            )
            .Join(
                this.context.MetadataItems,
                x => x.mi.MetadataItemId,
                m => m.Id,
                (x, m) => new { x.mp, m.LibrarySectionId }
            )
            .Where(x => x.LibrarySectionId == libraryId)
            .Select(x => x.mp.File)
            .Distinct()
            .AsAsyncEnumerable();

        return query;
    }
}
