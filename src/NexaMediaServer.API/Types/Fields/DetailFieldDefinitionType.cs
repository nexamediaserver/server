// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Represents a field definition for the GraphQL API.
/// </summary>
[GraphQLName("DetailFieldDefinition")]
public sealed class DetailFieldDefinitionType
{
    /// <summary>
    /// Gets the unique key identifying this field definition.
    /// </summary>
    /// <remarks>
    /// For standard fields, this is the field type name. For custom fields, this is the custom field key.
    /// </remarks>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of field (e.g., Title, Summary, Custom, etc.).
    /// </summary>
    public DetailFieldType FieldType { get; init; }

    /// <summary>
    /// Gets the display label for this field.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the recommended widget type for client-side rendering.
    /// </summary>
    public DetailFieldWidgetType Widget { get; init; }

    /// <summary>
    /// Gets the display order of this field.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Gets the custom field key for Custom field types.
    /// </summary>
    /// <remarks>
    /// Only set when <see cref="FieldType"/> is <see cref="DetailFieldType.Custom"/>.
    /// This key maps to <see cref="Core.Entities.MetadataItem.ExtraFields"/>.
    /// </remarks>
    public string? CustomFieldKey { get; init; }

    /// <summary>
    /// Gets the key of the group this field belongs to.
    /// </summary>
    /// <remarks>
    /// If null, the field is ungrouped and will be rendered in the default vertical layout.
    /// </remarks>
    public string? GroupKey { get; init; }
}
