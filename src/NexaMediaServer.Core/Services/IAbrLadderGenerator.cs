// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Generates adaptive bitrate ladders for transcoded streams.
/// </summary>
public interface IAbrLadderGenerator
{
    /// <summary>
    /// Generates an ABR ladder for the given source resolution and bitrate.
    /// </summary>
    /// <param name="sourceWidth">The source video width.</param>
    /// <param name="sourceHeight">The source video height.</param>
    /// <param name="sourceBitrate">The source video bitrate, if known.</param>
    /// <param name="maxAllowedBitrate">The maximum bitrate allowed by client capabilities.</param>
    /// <param name="includeSource">Whether to include the source resolution as a variant.</param>
    /// <returns>The generated ABR ladder.</returns>
    AbrLadder GenerateLadder(
        int sourceWidth,
        int sourceHeight,
        int? sourceBitrate,
        int maxAllowedBitrate,
        bool includeSource = true
    );
}
