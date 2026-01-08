// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides HLS manifest generation for media parts.
/// </summary>
public interface IHlsTranscodeService
{
    /// <summary>
    /// Ensures an HLS master playlist and segments exist for the given media part.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="abrLadder">The ABR ladder to use for variant generation, or null for single quality.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The master playlist and segment directory information.</returns>
    Task<HlsTranscodeResult> EnsureHlsAsync(
        int mediaPartId,
        AbrLadder? abrLadder,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Ensures an HLS variant playlist and segments exist for a specific quality variant.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="variant">The quality variant to generate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The variant playlist path.</returns>
    Task<string> EnsureVariantAsync(
        int mediaPartId,
        AbrVariant variant,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Generates an HLS manifest starting from a specific seek position.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="seekMs">The seek position in milliseconds to start transcoding from.</param>
    /// <param name="abrLadder">The ABR ladder to use for variant generation, or null for single quality.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The master playlist and segment directory information, plus the actual start time.</returns>
    Task<HlsSeekResult> EnsureHlsWithSeekAsync(
        int mediaPartId,
        long seekMs,
        AbrLadder? abrLadder,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets the path to a specific HLS segment file.
    /// </summary>
    /// <param name="mediaPartId">Target media part identifier.</param>
    /// <param name="variantId">The variant identifier (e.g., "720p").</param>
    /// <param name="segmentNumber">The segment number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The segment file path, or null if not found.</returns>
    Task<string?> GetSegmentPathAsync(
        int mediaPartId,
        string variantId,
        int segmentNumber,
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
