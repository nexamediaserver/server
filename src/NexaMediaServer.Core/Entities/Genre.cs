// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a genre classification for metadata items with support for hierarchical organization.
/// </summary>
public class Genre : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the genre.
    /// </summary>
    public Guid Uuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the genre name (e.g., "Action", "Science Fiction", "Progressive Rock").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent genre ID for hierarchical classification.
    /// </summary>
    /// <remarks>
    /// Null indicates this is a root-level genre. Supports unlimited depth.
    /// </remarks>
    public int? ParentGenreId { get; set; }

    /// <summary>
    /// Gets or sets the parent genre navigation property.
    /// </summary>
    public Genre? ParentGenre { get; set; }

    /// <summary>
    /// Gets or sets the collection of child genres.
    /// </summary>
    public ICollection<Genre> ChildGenres { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of metadata items associated with this genre.
    /// </summary>
    /// <remarks>
    /// Metadata items are only assigned to leaf genres, not to parent/child genres automatically.
    /// </remarks>
    public ICollection<MetadataItem> MetadataItems { get; set; } = [];
}
