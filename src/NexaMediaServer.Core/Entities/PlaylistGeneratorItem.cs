// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a single entry produced by a playlist generator.
/// </summary>
public class PlaylistGeneratorItem : AuditableEntity
{
    /// <summary>
    /// Gets or sets the owning generator identifier.
    /// </summary>
    public int PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the owning generator.
    /// </summary>
    public PlaylistGenerator PlaylistGenerator { get; set; } = null!;

    /// <summary>
    /// Gets or sets the metadata item identifier referenced by this entry.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the metadata item navigation property.
    /// </summary>
    public MetadataItem MetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the media item identifier when specific media is selected.
    /// </summary>
    public int? MediaItemId { get; set; }

    /// <summary>
    /// Gets or sets the media item navigation property.
    /// </summary>
    public MediaItem? MediaItem { get; set; }

    /// <summary>
    /// Gets or sets the media part identifier if a part is pre-selected.
    /// </summary>
    public int? MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the media part navigation property.
    /// </summary>
    public MediaPart? MediaPart { get; set; }

    /// <summary>
    /// Gets or sets the display or deterministic order of this entry.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry has been served to the client.
    /// </summary>
    public bool Served { get; set; }

    /// <summary>
    /// Gets or sets an optional cohort token used for repeat/shuffle tracking.
    /// </summary>
    public string? Cohort { get; set; }
}
