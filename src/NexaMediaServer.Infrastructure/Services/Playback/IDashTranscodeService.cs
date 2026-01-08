// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides DASH manifest generation for media parts.
/// </summary>
public interface IDashTranscodeService
{
    /// <summary>
    /// Ensures a DASH manifest and segments exist for the given media part.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manifest and segment directory information.</returns>
    Task<DashTranscodeResult> EnsureDashAsync(int mediaPartId, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a DASH manifest starting from a specific seek position.
    /// This will clear any existing cache and start a fresh transcode from the keyframe nearest to the seek position.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="seekMs">The seek position in milliseconds to start transcoding from.</param>
    /// <param name="startSegmentNumber">Optional segment number to start from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The manifest and segment directory information, plus the actual start time.</returns>
    Task<DashSeekResult> EnsureDashWithSeekAsync(
        int mediaPartId,
        long seekMs,
        int? startSegmentNumber,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Waits for a segment file to be created during an active transcode.
    /// </summary>
    /// <param name="segmentPath">Full path to the expected segment file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the segment file was created within the timeout; otherwise <c>false</c>.</returns>
    Task<bool> WaitForSegmentAsync(string segmentPath, CancellationToken cancellationToken);
}

/// <summary>
/// Result of a seek-based DASH transcode operation.
/// </summary>
/// <param name="ManifestPath">Path to the generated manifest file.</param>
/// <param name="OutputDirectory">Directory containing the segments.</param>
/// <param name="StartTimeMs">The actual start time of the transcoded content in milliseconds (keyframe-aligned).</param>
public sealed record DashSeekResult(string ManifestPath, string OutputDirectory, long StartTimeMs);
