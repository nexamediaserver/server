// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a relationship between two metadata items with an associated relation type.
/// </summary>
public class MetadataRelation : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the primary metadata item in the relationship.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the primary metadata item involved in the relationship.
    /// </summary>
    public MetadataItem MetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the related metadata item.
    /// </summary>
    public int RelatedMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the metadata item that is related to the primary item.
    /// </summary>
    public MetadataItem RelatedMetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets optional free-form text that further describes the relation (e.g., role name).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the type of relation between the metadata items.
    /// </summary>
    public RelationType RelationType { get; set; }
}
