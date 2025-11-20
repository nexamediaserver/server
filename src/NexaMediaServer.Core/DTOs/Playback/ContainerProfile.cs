// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Container-level conditions such as stream limits.
/// </summary>
public sealed class ContainerProfile
{
    /// <summary>
    /// Gets or sets the media type (Audio, Video).
    /// </summary>
    [Required]
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets conditional filters for this container.
    /// </summary>
    public List<ProfileCondition> Conditions { get; set; } = [];
}
