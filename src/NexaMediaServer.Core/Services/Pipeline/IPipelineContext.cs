// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services.Pipeline;

/// <summary>
/// Carries scan-scoped context and reporting hooks between pipeline stages.
/// </summary>
public interface IPipelineContext
{
    /// <summary>
    /// Gets the library section being scanned.
    /// </summary>
    LibrarySection LibrarySection { get; }

    /// <summary>
    /// Gets the mutable scan record for tracking progress.
    /// </summary>
    LibraryScan Scan { get; }

    /// <summary>
    /// Gets the checkpoint data for resuming an interrupted scan.
    /// Null if this is a fresh scan.
    /// </summary>
    ScanCheckpoint? Checkpoint { get; }

    /// <summary>
    /// Reports progress for the current stage.
    /// </summary>
    /// <param name="progress">Progress payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable progress operation.</returns>
    ValueTask ReportProgressAsync(
        ScanPipelineProgress progress,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Saves a checkpoint to allow scan resumption after a crash or restart.
    /// </summary>
    /// <param name="stage">The current pipeline stage name.</param>
    /// <param name="cursor">An opaque cursor value (e.g., last processed path or offset).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async save operation.</returns>
    ValueTask SaveCheckpointAsync(
        string stage,
        string? cursor,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Records a file path as "seen" during the current scan for orphan detection.
    /// Paths are buffered and flushed in batches for performance.
    /// </summary>
    /// <param name="paths">The file paths that were seen.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async record operation.</returns>
    ValueTask RecordSeenPathsAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Clears all seen paths and checkpoint data after a successful scan completion.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async clear operation.</returns>
    ValueTask ClearCheckpointAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Progress update payload for scan pipeline stages.
/// </summary>
/// <param name="Stage">Human-readable stage name.</param>
/// <param name="Processed">Count processed in this stage.</param>
/// <param name="Total">Optional total for the stage.</param>
public sealed record ScanPipelineProgress(string Stage, long Processed, long? Total = null);
