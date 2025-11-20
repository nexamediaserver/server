// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Direct play declaration for a specific container/codec combination.
/// </summary>
public sealed class DirectPlayProfile
{
    /// <summary>
    /// Gets or sets the media type (Audio, Video).
    /// </summary>
    [Required]
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the supported container(s), comma separated.
    /// </summary>
    [Required]
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported video codecs, comma separated.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the supported audio codecs, comma separated.
    /// </summary>
    public string? AudioCodec { get; set; }
}
