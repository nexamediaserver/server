// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents user-specific settings and playback metadata for a metadata item.
/// </summary>
public class MetadataItemSetting : AuditableEntity
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
    /// Gets or sets the identifier of the related metadata item.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the related metadata item.
    /// </summary>
    public MetadataItem MetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user rating for the item.
    /// </summary>
    public float Rating { get; set; }

    /// <summary>
    /// Gets or sets the last playback position offset for resume.
    /// </summary>
    public int ViewOffset { get; set; }

    /// <summary>
    /// Gets or sets the number of times the item was viewed.
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the item was last viewed.
    /// </summary>
    public DateTime? LastViewedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times playback was skipped.
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when playback was last skipped.
    /// </summary>
    public DateTime? LastSkippedAt { get; set; }
}
