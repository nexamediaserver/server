// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Transcoding target the client can consume.
/// </summary>
public sealed class TranscodingProfile
{
    /// <summary>
    /// Gets or sets the media type (Audio, Video).
    /// </summary>
    [Required]
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the output container.
    /// </summary>
    [Required]
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the playback context (e.g., Streaming, Static).
    /// </summary>
    public string Context { get; set; } = "Streaming";

    /// <summary>
    /// Gets or sets the delivery protocol (e.g., hls).
    /// </summary>
    public string Protocol { get; set; } = "hls";

    /// <summary>
    /// Gets or sets the allowed audio codecs, comma separated.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the allowed video codecs, comma separated.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the maximum audio channels allowed for this profile.
    /// </summary>
    public string? MaxAudioChannels { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be copied.
    /// </summary>
    public bool? CopyTimestamps { get; set; }

    /// <summary>
    /// Gets or sets extra conditions that must apply for this profile.
    /// </summary>
    public List<ProfileCondition> ApplyConditions { get; set; } = [];
}
