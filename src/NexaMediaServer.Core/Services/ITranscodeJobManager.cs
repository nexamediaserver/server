// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Manages transcode job lifecycle, tracking, and resource throttling.
/// </summary>
public interface ITranscodeJobManager
{
    /// <summary>
    /// Creates a new transcode job for the specified playback session and media part.
    /// </summary>
    /// <param name="playbackSessionId">The playback session database identifier.</param>
    /// <param name="mediaPartId">The media part identifier to transcode.</param>
    /// <param name="protocol">The streaming protocol (dash, hls).</param>
    /// <param name="outputPath">The base output path for segments.</param>
    /// <param name="options">Optional transcode options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transcode job.</returns>
    Task<TranscodeJob> CreateJobAsync(
        int playbackSessionId,
        int mediaPartId,
        string protocol,
        string outputPath,
        TranscodeJobOptions? options,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Marks a job as started and records the FFmpeg process ID.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="processId">The FFmpeg process identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartJobAsync(int jobId, int processId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the progress of a running transcode job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="progress">The progress percentage (0-100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReportProgressAsync(int jobId, double progress, CancellationToken cancellationToken);

    /// <summary>
    /// Pings a job to reset its kill timer and keep it alive.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PingJobAsync(int jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a job as completed.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteJobAsync(int jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a job and kills the associated FFmpeg process.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="deleteSegments">Whether to delete output segments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CancelJobAsync(int jobId, bool deleteSegments, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a job as failed with an error message.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailJobAsync(int jobId, string errorMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a job by its identifier.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcode job, or null if not found.</returns>
    Task<TranscodeJob?> GetJobAsync(int jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an active job for the specified playback session and media part.
    /// </summary>
    /// <param name="playbackSessionId">The playback session identifier.</param>
    /// <param name="mediaPartId">The media part identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active job, or null if none exists.</returns>
    Task<TranscodeJob?> GetActiveJobAsync(
        int playbackSessionId,
        int mediaPartId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets all active jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active transcode jobs.</returns>
    Task<IReadOnlyList<TranscodeJob>> GetActiveJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cancels all jobs for a playback session.
    /// </summary>
    /// <param name="playbackSessionId">The playback session identifier.</param>
    /// <param name="deleteSegments">Whether to delete output segments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of jobs cancelled.</returns>
    Task<int> CancelJobsForSessionAsync(
        int playbackSessionId,
        bool deleteSegments,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets the current number of running transcode jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of running jobs.</returns>
    Task<int> GetRunningJobCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a new transcode job can be started based on throttling limits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a new job can be started; otherwise <c>false</c>.</returns>
    Task<bool> CanStartNewJobAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up stale jobs on startup (marks as failed, deletes segments).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of jobs cleaned up.</returns>
    Task<int> CleanupStaleJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Kills idle jobs that haven't been pinged within the timeout.
    /// </summary>
    /// <param name="timeoutSeconds">The idle timeout in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of jobs killed.</returns>
    Task<int> KillIdleJobsAsync(int timeoutSeconds, CancellationToken cancellationToken);
}

/// <summary>
/// Options for creating a transcode job.
/// </summary>
public sealed class TranscodeJobOptions
{
    /// <summary>
    /// Gets or sets the seek offset in milliseconds.
    /// </summary>
    public long? SeekOffsetMs { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate.
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target video width.
    /// </summary>
    public int? VideoWidth { get; set; }

    /// <summary>
    /// Gets or sets the target video height.
    /// </summary>
    public int? VideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the target audio bitrate.
    /// </summary>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio channel count.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the selected audio stream index.
    /// </summary>
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected subtitle stream index for burn-in.
    /// </summary>
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use hardware acceleration.
    /// </summary>
    public bool UseHardwareAcceleration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable tone mapping.
    /// </summary>
    public bool EnableToneMapping { get; set; }
}
