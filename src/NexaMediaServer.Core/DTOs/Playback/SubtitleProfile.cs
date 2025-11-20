// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Subtitle format preferences and delivery mode.
/// </summary>
public sealed class SubtitleProfile
{
    /// <summary>
    /// Gets or sets the subtitle format.
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method used to deliver subtitles.
    /// </summary>
    [Required]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery protocol, if constrained (e.g., hls, dash).
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets the languages applicable to this profile.
    /// </summary>
    public List<string> Languages { get; set; } = [];
}
