// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
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
    /// Reports progress for the current stage.
    /// </summary>
    /// <param name="progress">Progress payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable progress operation.</returns>
    ValueTask ReportProgressAsync(
        ScanPipelineProgress progress,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Progress update payload for scan pipeline stages.
/// </summary>
/// <param name="Stage">Human-readable stage name.</param>
/// <param name="Processed">Count processed in this stage.</param>
/// <param name="Total">Optional total for the stage.</param>
public sealed record ScanPipelineProgress(string Stage, long Processed, long? Total = null);
