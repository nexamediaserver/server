// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents an admin-defined custom field definition for item detail pages.
/// </summary>
/// <remarks>
/// Custom fields allow administrators to define additional metadata fields
/// that can be displayed on item detail pages. The actual values are stored
/// in <see cref="MetadataItem.ExtraFields"/> using the <see cref="Key"/> as the dictionary key.
/// </remarks>
public class CustomFieldDefinition : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique key identifier for this field.
    /// </summary>
    /// <remarks>
    /// This key is used as the dictionary key in <see cref="MetadataItem.ExtraFields"/>.
    /// Must be unique and should follow a consistent naming convention (e.g., snake_case).
    /// </remarks>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display label for this field.
    /// </summary>
    /// <remarks>
    /// This is the user-facing label shown on the detail page.
    /// </remarks>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Gets or sets the widget type for rendering this field.
    /// </summary>
    public DetailFieldWidgetType Widget { get; set; }

    /// <summary>
    /// Gets or sets the metadata types this field applies to.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON array. If empty, the field applies to all metadata types.
    /// </remarks>
    public List<MetadataType> ApplicableMetadataTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the display order of this field.
    /// </summary>
    /// <remarks>
    /// Custom fields are typically displayed after standard fields.
    /// Lower values appear first.
    /// </remarks>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this field is enabled.
    /// </summary>
    /// <remarks>
    /// Disabled fields are not returned in field definitions but their data
    /// in <see cref="MetadataItem.ExtraFields"/> is preserved.
    /// </remarks>
    public bool IsEnabled { get; set; } = true;
}
