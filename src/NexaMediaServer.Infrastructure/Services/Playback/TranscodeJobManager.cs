// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

using IO = System.IO;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Manages transcode job lifecycle, tracking, and resource throttling.
/// </summary>
public partial class TranscodeJobManager : ITranscodeJobManager
{
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<TranscodeJobManager> logger;
    private readonly TranscodeOptions options;

    // In-memory tracking of active FFmpeg processes for fast lookup
    private readonly ConcurrentDictionary<int, int> activeProcesses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeJobManager"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating database contexts.</param>
    /// <param name="logger">Typed logger.</param>
    /// <param name="options">Transcode configuration options.</param>
    public TranscodeJobManager(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ILogger<TranscodeJobManager> logger,
        IOptions<TranscodeOptions> options
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
        this.options = options.Value;
    }

    /// <inheritdoc />
    public async Task<TranscodeJob> CreateJobAsync(
        int playbackSessionId,
        int mediaPartId,
        string protocol,
        string outputPath,
        TranscodeJobOptions? options,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = new TranscodeJob
        {
            PlaybackSessionId = playbackSessionId,
            MediaPartId = mediaPartId,
            Protocol = protocol,
            OutputPath = outputPath,
            State = TranscodeJobState.Pending,
            Progress = 0,
            LastPingAt = DateTime.UtcNow,
            SeekOffsetMs = options?.SeekOffsetMs,
            VideoBitrate = options?.VideoBitrate,
            VideoWidth = options?.VideoWidth,
            VideoHeight = options?.VideoHeight,
            AudioBitrate = options?.AudioBitrate,
            AudioChannels = options?.AudioChannels,
            AudioStreamIndex = options?.AudioStreamIndex,
            SubtitleStreamIndex = options?.SubtitleStreamIndex,
            UseHardwareAcceleration = options?.UseHardwareAcceleration ?? false,
            EnableToneMapping = options?.EnableToneMapping ?? false,
        };

        await db.TranscodeJobs.AddAsync(job, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        this.LogJobCreated(job.Id, mediaPartId, protocol);

        return job;
    }

    /// <inheritdoc />
    public async Task StartJobAsync(int jobId, int processId, CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
        {
            return;
        }

        job.State = TranscodeJobState.Running;
        job.FfmpegProcessId = processId;
        job.StartedAt = DateTime.UtcNow;
        job.LastPingAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        this.activeProcesses[jobId] = processId;
        this.LogJobStarted(jobId, processId);
    }

    /// <inheritdoc />
    public async Task ReportProgressAsync(
        int jobId,
        double progress,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null || job.State != TranscodeJobState.Running)
        {
            return;
        }

        job.Progress = Math.Clamp(progress, 0, 100);
        job.LastPingAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PingJobAsync(int jobId, CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null || job.State != TranscodeJobState.Running)
        {
            return;
        }

        job.LastPingAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CompleteJobAsync(int jobId, CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
        {
            return;
        }

        job.State = TranscodeJobState.Completed;
        job.Progress = 100;
        job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        this.activeProcesses.TryRemove(jobId, out _);
        this.LogJobCompleted(jobId);
    }

    /// <inheritdoc />
    public async Task CancelJobAsync(
        int jobId,
        bool deleteSegments,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
        {
            return;
        }

        // Kill FFmpeg process if running
        if (job.FfmpegProcessId.HasValue)
        {
            this.TryKillProcess(job.FfmpegProcessId.Value);
        }

        job.State = TranscodeJobState.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        this.activeProcesses.TryRemove(jobId, out _);

        if (deleteSegments && !string.IsNullOrEmpty(job.OutputPath))
        {
            this.TryDeleteSegments(job.OutputPath);
        }

        this.LogJobCancelled(jobId);
    }

    /// <inheritdoc />
    public async Task FailJobAsync(
        int jobId,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
        {
            return;
        }

        // Kill FFmpeg process if running
        if (job.FfmpegProcessId.HasValue)
        {
            this.TryKillProcess(job.FfmpegProcessId.Value);
        }

        job.State = TranscodeJobState.Failed;
        job.ErrorMessage = errorMessage;
        job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        this.activeProcesses.TryRemove(jobId, out _);
        this.LogJobFailed(jobId, errorMessage);
    }

    /// <inheritdoc />
    public async Task<TranscodeJob?> GetJobAsync(int jobId, CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TranscodeJobs.FindAsync([jobId], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TranscodeJob?> GetActiveJobAsync(
        int playbackSessionId,
        int mediaPartId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db
            .TranscodeJobs.Where(j =>
                j.PlaybackSessionId == playbackSessionId
                && j.MediaPartId == mediaPartId
                && (j.State == TranscodeJobState.Pending || j.State == TranscodeJobState.Running)
            )
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TranscodeJob>> GetActiveJobsAsync(
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db
            .TranscodeJobs.Where(j =>
                j.State == TranscodeJobState.Pending || j.State == TranscodeJobState.Running
            )
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CancelJobsForSessionAsync(
        int playbackSessionId,
        bool deleteSegments,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var jobs = await db
            .TranscodeJobs.Where(j =>
                j.PlaybackSessionId == playbackSessionId
                && (j.State == TranscodeJobState.Pending || j.State == TranscodeJobState.Running)
            )
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            if (job.FfmpegProcessId.HasValue)
            {
                this.TryKillProcess(job.FfmpegProcessId.Value);
            }

            job.State = TranscodeJobState.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            this.activeProcesses.TryRemove(job.Id, out _);

            if (deleteSegments && !string.IsNullOrEmpty(job.OutputPath))
            {
                this.TryDeleteSegments(job.OutputPath);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        this.LogJobsCancelledForSession(playbackSessionId, jobs.Count);

        return jobs.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetRunningJobCountAsync(CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db
            .TranscodeJobs.CountAsync(
                j => j.State == TranscodeJobState.Running,
                cancellationToken
            );
    }

    /// <inheritdoc />
    public async Task<bool> CanStartNewJobAsync(CancellationToken cancellationToken)
    {
        var runningCount = await this.GetRunningJobCountAsync(cancellationToken);
        return runningCount < this.options.MaxConcurrentTranscodes;
    }

    /// <inheritdoc />
    public async Task<int> CleanupStaleJobsAsync(CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Find all jobs that were running when the server was last shutdown
        var staleJobs = await db
            .TranscodeJobs.Where(j =>
                j.State == TranscodeJobState.Pending || j.State == TranscodeJobState.Running
            )
            .ToListAsync(cancellationToken);

        foreach (var job in staleJobs)
        {
            // Kill any orphaned processes (unlikely to still exist after restart)
            if (job.FfmpegProcessId.HasValue)
            {
                this.TryKillProcess(job.FfmpegProcessId.Value);
            }

            job.State = TranscodeJobState.Failed;
            job.ErrorMessage = "Server restart - job was interrupted";
            job.CompletedAt = DateTime.UtcNow;

            // Delete orphaned segments
            if (!string.IsNullOrEmpty(job.OutputPath))
            {
                this.TryDeleteSegments(job.OutputPath);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (staleJobs.Count > 0)
        {
            this.LogStaleJobsCleanedUp(staleJobs.Count);
        }

        return staleJobs.Count;
    }

    /// <inheritdoc />
    public async Task<int> KillIdleJobsAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var cutoff = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        var idleJobs = await db
            .TranscodeJobs.Where(j =>
                j.State == TranscodeJobState.Running && j.LastPingAt < cutoff
            )
            .ToListAsync(cancellationToken);

        foreach (var job in idleJobs)
        {
            if (job.FfmpegProcessId.HasValue)
            {
                this.TryKillProcess(job.FfmpegProcessId.Value);
            }

            job.State = TranscodeJobState.Cancelled;
            job.ErrorMessage = "Idle timeout - no heartbeat received";
            job.CompletedAt = DateTime.UtcNow;

            this.activeProcesses.TryRemove(job.Id, out _);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (idleJobs.Count > 0)
        {
            this.LogIdleJobsKilled(idleJobs.Count, timeoutSeconds);
        }

        return idleJobs.Count;
    }

    private void TryKillProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                this.LogProcessKilled(processId);
            }
        }
        catch (ArgumentException)
        {
            // Process doesn't exist
        }
        catch (Exception ex)
        {
            this.LogProcessKillFailed(processId, ex);
        }
    }

    private void TryDeleteSegments(string outputPath)
    {
        try
        {
            if (IO.Directory.Exists(outputPath))
            {
                IO.Directory.Delete(outputPath, recursive: true);
                this.LogSegmentsDeleted(outputPath);
            }
        }
        catch (Exception ex)
        {
            this.LogSegmentDeleteFailed(outputPath, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created transcode job {JobId} for part {MediaPartId} ({Protocol})")]
    private partial void LogJobCreated(int jobId, int mediaPartId, string protocol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started transcode job {JobId} with FFmpeg process {ProcessId}")]
    private partial void LogJobStarted(int jobId, int processId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed transcode job {JobId}")]
    private partial void LogJobCompleted(int jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled transcode job {JobId}")]
    private partial void LogJobCancelled(int jobId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transcode job {JobId} failed: {ErrorMessage}")]
    private partial void LogJobFailed(int jobId, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled {Count} transcode jobs for playback session {SessionId}")]
    private partial void LogJobsCancelledForSession(int sessionId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaned up {Count} stale transcode jobs from previous server run")]
    private partial void LogStaleJobsCleanedUp(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Killed {Count} idle transcode jobs (timeout: {Timeout}s)")]
    private partial void LogIdleJobsKilled(int count, int timeout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Killed FFmpeg process {ProcessId}")]
    private partial void LogProcessKilled(int processId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to kill FFmpeg process {ProcessId}")]
    private partial void LogProcessKillFailed(int processId, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted transcode segments at {Path}")]
    private partial void LogSegmentsDeleted(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete transcode segments at {Path}")]
    private partial void LogSegmentDeleteFailed(string path, Exception ex);
}
