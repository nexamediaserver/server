// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input type for updating a custom field definition.
/// </summary>
[GraphQLName("UpdateCustomFieldDefinitionInput")]
public sealed class UpdateCustomFieldDefinitionInput
{
    /// <summary>
    /// Gets or sets the ID of the custom field definition to update.
    /// </summary>
    [ID]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the new display label for this field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the new widget type for rendering this field.
    /// </summary>
    public DetailFieldWidgetType? Widget { get; set; }

    /// <summary>
    /// Gets or sets the new metadata types this field applies to.
    /// Empty list means the field applies to all metadata types.
    /// </summary>
    public IReadOnlyList<MetadataType>? ApplicableMetadataTypes { get; set; }

    /// <summary>
    /// Gets or sets the new display order of this field.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the field is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}
