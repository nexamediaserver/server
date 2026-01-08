// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// In-memory cache for active transcode jobs, providing fast lookup and process management.
/// Backed by the database <see cref="Entities.TranscodeJob"/> entity for persistence.
/// </summary>
public interface ITranscodeJobCache
{
    /// <summary>
    /// Registers an active transcode job in the cache.
    /// </summary>
    /// <param name="jobId">The database job identifier.</param>
    /// <param name="outputPath">The output directory path for segments.</param>
    /// <param name="process">The FFmpeg process handle.</param>
    /// <param name="cancellationTokenSource">Token source to cancel the transcode.</param>
    void Register(int jobId, string outputPath, Process process, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Removes a job from the cache when it completes or is cancelled.
    /// </summary>
    /// <param name="jobId">The job identifier to remove.</param>
    void Unregister(int jobId);

    /// <summary>
    /// Gets an active job by its output path.
    /// </summary>
    /// <param name="outputPath">The output directory path.</param>
    /// <returns>The cached job entry, or null if not found.</returns>
    TranscodeJobCacheEntry? GetByPath(string outputPath);

    /// <summary>
    /// Gets an active job by its identifier.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>The cached job entry, or null if not found.</returns>
    TranscodeJobCacheEntry? GetById(int jobId);

    /// <summary>
    /// Updates the last access time for a job (for LRU tracking).
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    void Touch(int jobId);

    /// <summary>
    /// Kills an active transcode by its output path and waits for process termination.
    /// </summary>
    /// <param name="outputPath">The output directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a job was found and killed; false otherwise.</returns>
    Task<bool> KillByPathAsync(string outputPath, CancellationToken cancellationToken);

    /// <summary>
    /// Kills an active transcode by its job identifier and waits for process termination.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a job was found and killed; false otherwise.</returns>
    Task<bool> KillByIdAsync(int jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all active job entries.
    /// </summary>
    /// <returns>A read-only collection of all cached job entries.</returns>
    IReadOnlyCollection<TranscodeJobCacheEntry> GetAll();

    /// <summary>
    /// Removes jobs that haven't been accessed within the specified timeout.
    /// Called periodically by a background cleanup timer.
    /// </summary>
    /// <param name="idleTimeout">The idle timeout duration.</param>
    /// <returns>The number of jobs evicted.</returns>
    int EvictIdle(TimeSpan idleTimeout);

    /// <summary>
    /// Gets the current segment index that the transcoder has reached for a given output path.
    /// This scans the output directory to find the highest numbered segment file.
    /// </summary>
    /// <param name="outputPath">The output directory path.</param>
    /// <returns>The highest segment index found, or null if no segments exist or job is not active.</returns>
    int? GetCurrentTranscodingIndex(string outputPath);
}

/// <summary>
/// An entry in the transcode job cache representing an active FFmpeg process.
/// </summary>
public sealed class TranscodeJobCacheEntry
{
    /// <summary>
    /// Gets the database job identifier.
    /// </summary>
    public required int JobId { get; init; }

    /// <summary>
    /// Gets the output directory path for segments.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets the FFmpeg process handle.
    /// </summary>
    public required Process Process { get; init; }

    /// <summary>
    /// Gets the cancellation token source to cancel the transcode.
    /// </summary>
    public required CancellationTokenSource CancellationTokenSource { get; init; }

    /// <summary>
    /// Gets or sets the last access time for LRU tracking.
    /// </summary>
    public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the time when the job was registered.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the segment length in seconds used for this transcode.
    /// </summary>
    public int SegmentLengthSeconds { get; set; } = 6;

    /// <summary>
    /// Gets or sets the start time offset in milliseconds (for seek-based transcodes).
    /// </summary>
    public long StartTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the segment file extension (e.g., ".m4s", ".ts").
    /// </summary>
    public string SegmentExtension { get; set; } = ".m4s";

    /// <summary>
    /// Gets or sets the segment filename prefix (e.g., "chunk-stream0-").
    /// </summary>
    public string SegmentPrefix { get; set; } = "chunk-stream";
}
