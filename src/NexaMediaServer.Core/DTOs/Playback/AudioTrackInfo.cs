// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents information about a selectable audio track.
/// </summary>
public sealed class AudioTrackInfo
{
    /// <summary>
    /// Gets or sets the stream index within the container.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 639 language code.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display title (e.g., "English 5.1").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Gets or sets the bitrate in bits per second.
    /// </summary>
    public int Bitrate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default track.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a forced track.
    /// </summary>
    public bool IsForced { get; set; }
}
