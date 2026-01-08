// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents information about a selectable subtitle track.
/// </summary>
public sealed class SubtitleTrackInfo
{
    /// <summary>
    /// Gets or sets the stream index within the container.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the subtitle codec.
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 639 language code.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display title (e.g., "English SDH").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is the default track.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a forced track.
    /// </summary>
    public bool IsForced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this track contains SDH/CC content.
    /// </summary>
    public bool IsHearingImpaired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an external sidecar subtitle.
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// Gets or sets the external subtitle file path, if applicable.
    /// </summary>
    public string? ExternalPath { get; set; }
}
