// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Hangfire job that cleans up orphaned transcode segments.
/// </summary>
public sealed partial class SegmentCleanupJob
{
    private readonly ITranscodeJobManager transcodeJobManager;
    private readonly IApplicationPaths paths;
    private readonly ILogger<SegmentCleanupJob> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentCleanupJob"/> class.
    /// </summary>
    /// <param name="transcodeJobManager">Transcode job manager.</param>
    /// <param name="paths">Application paths provider.</param>
    /// <param name="logger">Typed logger.</param>
    public SegmentCleanupJob(
        ITranscodeJobManager transcodeJobManager,
        IApplicationPaths paths,
        ILogger<SegmentCleanupJob> logger
    )
    {
        this.transcodeJobManager = transcodeJobManager;
        this.paths = paths;
        this.logger = logger;
    }

    /// <summary>
    /// Registers the recurring cleanup job with Hangfire.
    /// </summary>
    /// <param name="recurringJobManager">Hangfire recurring job manager.</param>
    public static void Register(IRecurringJobManager recurringJobManager)
    {
        // Run daily at 3 AM UTC
        recurringJobManager.AddOrUpdate<SegmentCleanupJob>(
            "segment-cleanup",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3)
        );
    }

    /// <summary>
    /// Executes the cleanup job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("metadata_agents")]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogCleanupStarted();

        int segmentDirsDeleted = 0;

        // Cleanup orphaned DASH segments
        var dashDir = Path.Combine(this.paths.CacheDirectory, "dash");
        segmentDirsDeleted += await this.CleanupOrphanedSegmentsAsync(
            dashDir,
            TimeSpan.FromDays(7),
            cancellationToken
        );

        var dashSeekDir = Path.Combine(this.paths.CacheDirectory, "dash-seek");
        segmentDirsDeleted += await this.CleanupOrphanedSegmentsAsync(
            dashSeekDir,
            TimeSpan.FromDays(1), // Seek segments are more transient
            cancellationToken
        );

        // Cleanup orphaned HLS segments
        var hlsDir = Path.Combine(this.paths.CacheDirectory, "hls");
        segmentDirsDeleted += await this.CleanupOrphanedSegmentsAsync(
            hlsDir,
            TimeSpan.FromDays(7),
            cancellationToken
        );

        var hlsSeekDir = Path.Combine(this.paths.CacheDirectory, "hls-seek");
        segmentDirsDeleted += await this.CleanupOrphanedSegmentsAsync(
            hlsSeekDir,
            TimeSpan.FromDays(1), // Seek segments are more transient
            cancellationToken
        );

        // Also cleanup stale transcode job records from database
        int staleJobsCleaned = await this.transcodeJobManager.CleanupStaleJobsAsync(
            cancellationToken
        );

        this.LogCleanupCompleted(segmentDirsDeleted, staleJobsCleaned);
    }

    private async Task<int> CleanupOrphanedSegmentsAsync(
        string baseDir,
        TimeSpan maxAge,
        CancellationToken cancellationToken
    )
    {
        if (!Directory.Exists(baseDir))
        {
            return 0;
        }

        int dirsDeleted = 0;
        var cutoff = DateTime.UtcNow - maxAge;

        try
        {
            // Enumerate metadata UUID directories
            foreach (var metadataDir in Directory.EnumerateDirectories(baseDir))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Check if directory is old enough to consider for cleanup
                var dirInfo = new DirectoryInfo(metadataDir);
                if (dirInfo.LastWriteTimeUtc > cutoff)
                {
                    continue;
                }

                // Check if any files were recently modified
                bool hasRecentFiles = false;
                try
                {
                    foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        if (file.LastWriteTimeUtc > cutoff)
                        {
                            hasRecentFiles = true;
                            break;
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    this.LogDirectoryAccessError(metadataDir, ex);
                    continue;
                }

                if (!hasRecentFiles)
                {
                    try
                    {
                        Directory.Delete(metadataDir, recursive: true);
                        dirsDeleted++;
                        this.LogDirectoryDeleted(metadataDir);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        this.LogDirectoryDeleteError(metadataDir, ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            this.LogBaseDirectoryAccessError(baseDir, ex);
        }

        return dirsDeleted;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting segment cleanup job")]
    private partial void LogCleanupStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Segment cleanup completed: {DirsDeleted} directories deleted, {StaleJobsCleaned} stale jobs cleaned")]
    private partial void LogCleanupCompleted(int dirsDeleted, int staleJobsCleaned);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted orphaned segment directory: {Path}")]
    private partial void LogDirectoryDeleted(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to access directory for cleanup: {Path}")]
    private partial void LogDirectoryAccessError(string path, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete orphaned segment directory: {Path}")]
    private partial void LogDirectoryDeleteError(string path, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to access base cleanup directory: {Path}")]
    private partial void LogBaseDirectoryAccessError(string path, Exception ex);
}
