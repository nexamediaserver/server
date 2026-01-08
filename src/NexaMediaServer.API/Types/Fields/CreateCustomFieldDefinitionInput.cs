// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input type for creating a custom field definition.
/// </summary>
[GraphQLName("CreateCustomFieldDefinitionInput")]
public sealed class CreateCustomFieldDefinitionInput
{
    /// <summary>
    /// Gets or sets the unique key identifier for this field.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display label for this field.
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Gets or sets the widget type for rendering this field.
    /// </summary>
    public DetailFieldWidgetType Widget { get; set; }

    /// <summary>
    /// Gets or sets the metadata types this field applies to.
    /// Empty list means the field applies to all metadata types.
    /// </summary>
    public IReadOnlyList<MetadataType>? ApplicableMetadataTypes { get; set; }

    /// <summary>
    /// Gets or sets the display order of this field.
    /// </summary>
    public int SortOrder { get; set; }
}
