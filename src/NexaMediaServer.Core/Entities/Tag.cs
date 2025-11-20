// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a subjective content tag for metadata items.
/// </summary>
/// <remarks>
/// Tags describe subjective content characteristics such as "Christmas", "Female Protagonist",
/// "Adapted from a Book", etc., and are distinct from objective genre classifications.
/// </remarks>
public class Tag : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the tag.
    /// </summary>
    public Guid Uuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the tag name (e.g., "Christmas", "Female Protagonist", "Book Adaptation").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of metadata items associated with this tag.
    /// </summary>
    public ICollection<MetadataItem> MetadataItems { get; set; } = [];
}
