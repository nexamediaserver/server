// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Codec-specific conditions such as level, profile or color format.
/// </summary>
public sealed class CodecProfile
{
    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    [Required]
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the codec name, if this condition applies to a specific codec.
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the container restriction, if any.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the conditions to satisfy.
    /// </summary>
    public List<ProfileCondition> Conditions { get; set; } = [];
}
