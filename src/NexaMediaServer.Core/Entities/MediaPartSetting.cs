// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents user-specific settings for a media part, such as selected audio and subtitle streams.
/// </summary>
public class MediaPartSetting : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the user associated with this setting.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user associated with this setting.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the related media part.
    /// </summary>
    public int MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the related media part.
    /// </summary>
    public MediaPart MediaPart { get; set; } = null!;

    /// <summary>
    /// Gets or sets the index of the selected audio stream; null to use the default.
    /// </summary>
    public int? SelectedAudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the index of the selected subtitle stream; null to disable or use the default.
    /// </summary>
    public int? SelectedSubtitleStreamIndex { get; set; }
}
