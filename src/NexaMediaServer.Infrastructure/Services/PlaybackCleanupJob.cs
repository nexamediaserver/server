// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.IO;
using System.Linq;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

using IO = System.IO;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Recurring job that removes expired playback sessions and cleans up cached DASH assets.
/// </summary>
public sealed class PlaybackCleanupJob
{
    private static readonly Action<ILogger, string, Exception?> LogDashDeleteFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogDashDeleteFailed)),
            "Failed to delete DASH cache at {Directory}"
        );

    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<PlaybackCleanupJob> logger;
    private readonly IApplicationPaths paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackCleanupJob"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating media server DbContexts.</param>
    /// <param name="paths">Application paths provider.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaybackCleanupJob(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IApplicationPaths paths,
        ILogger<PlaybackCleanupJob> logger
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.paths = paths;
        this.logger = logger;
    }

    /// <summary>
    /// Executes cleanup for stale playback sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the cleanup operation.</returns>
    [Queue("playback")]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var staleSessions = await db
            .PlaybackSessions.Include(p => p.PlaylistGenerator)
            .Include(p => p.CurrentMediaPart)
            .Where(p => p.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (staleSessions.Count == 0)
        {
            return;
        }

        foreach (var playback in staleSessions)
        {
            if (playback.CurrentMediaPart != null)
            {
                await this.TryDeleteDashCachesAsync(
                    db,
                    playback.CurrentMediaPart,
                    playback.PlaybackSessionId,
                    cancellationToken
                );
            }

            if (playback.PlaylistGenerator != null)
            {
                db.PlaylistGenerators.Remove(playback.PlaylistGenerator);
            }

            db.PlaybackSessions.Remove(playback);
        }

        // Clean orphaned playlist generators that have expired (defensive cleanup)
        var staleGenerators = await db
            .PlaylistGenerators.Where(pg => pg.ExpiresAt <= now && pg.PlaybackSession == null)
            .ToListAsync(cancellationToken);
        if (staleGenerators.Count > 0)
        {
            db.PlaylistGenerators.RemoveRange(staleGenerators);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> ResolvePartIndexAsync(
        MediaServerContext db,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        var partIds = await db
            .MediaParts.Where(p => p.MediaItemId == part.MediaItemId)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var index = partIds.FindIndex(id => id == part.Id);
        return index < 0 ? 0 : index;
    }

    private async Task TryDeleteDashCachesAsync(
        MediaServerContext db,
        MediaPart part,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    )
    {
        var metadataItem = part.MediaItem?.MetadataItem
            ?? await db
                .MediaParts.Where(p => p.Id == part.Id)
                .Select(p => p.MediaItem!.MetadataItem)
                .FirstOrDefaultAsync(cancellationToken);

        if (metadataItem == null)
        {
            return;
        }

        bool hasOtherActiveSessions = await db.PlaybackSessions.AnyAsync(
            p =>
                p.PlaybackSessionId != playbackSessionId
                && p.CurrentMediaPartId == part.Id
                && p.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (hasOtherActiveSessions)
        {
            return;
        }

        var metadataUuid = metadataItem.Uuid.ToString("N", CultureInfo.InvariantCulture);
        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken);

        var targets = new[]
        {
            Path.Combine(
                this.paths.CacheDirectory,
                "dash",
                metadataUuid,
                partIndex.ToString(CultureInfo.InvariantCulture)
            ),
            Path.Combine(
                this.paths.CacheDirectory,
                "dash-seek",
                metadataUuid,
                partIndex.ToString(CultureInfo.InvariantCulture)
            ),
        };

        foreach (string directory in targets)
        {
            try
            {
                if (IO.Directory.Exists(directory))
                {
                    // Skip directories that have been recently modified (likely active transcode)
                    var dirInfo = new DirectoryInfo(directory);
                    if (dirInfo.LastWriteTimeUtc > DateTime.UtcNow.AddMinutes(-5))
                    {
                        continue;
                    }

                    IO.Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                LogDashDeleteFailed(this.logger, directory, ex);
            }
        }
    }

}
