// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents the adaptive bitrate ladder for a stream.
/// </summary>
public sealed class AbrLadder
{
    /// <summary>
    /// Gets or sets the list of quality variants, ordered from highest to lowest quality.
    /// </summary>
    public List<AbrVariant> Variants { get; set; } = [];

    /// <summary>
    /// Gets or sets the source video width.
    /// </summary>
    public int SourceWidth { get; set; }

    /// <summary>
    /// Gets or sets the source video height.
    /// </summary>
    public int SourceHeight { get; set; }

    /// <summary>
    /// Gets or sets the source video bitrate, if known.
    /// </summary>
    public int? SourceBitrate { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed bitrate based on client capabilities.
    /// </summary>
    public int MaxAllowedBitrate { get; set; }
}
