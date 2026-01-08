// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents a single quality variant in an adaptive bitrate ladder.
/// </summary>
public sealed class AbrVariant
{
    /// <summary>
    /// Gets or sets the variant identifier (e.g., "1080p", "720p", "480p").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label for the variant.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target video width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the target video height.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate in bits per second.
    /// </summary>
    public int VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio bitrate in bits per second.
    /// </summary>
    public int AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio channel count.
    /// </summary>
    public int AudioChannels { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether this is the source quality (no scaling).
    /// </summary>
    public bool IsSource { get; set; }

    /// <summary>
    /// Gets the total bitrate (video + audio).
    /// </summary>
    public int TotalBitrate => this.VideoBitrate + this.AudioBitrate;
}
