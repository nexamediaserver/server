// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Represents a custom field definition for the GraphQL API.
/// </summary>
[GraphQLName("CustomFieldDefinition")]
public sealed class CustomFieldDefinitionType
{
    /// <summary>
    /// Gets the unique identifier of the custom field definition.
    /// </summary>
    [ID]
    public int Id { get; init; }

    /// <summary>
    /// Gets the unique key identifier for this field.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display label for this field.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the widget type for rendering this field.
    /// </summary>
    public DetailFieldWidgetType Widget { get; init; }

    /// <summary>
    /// Gets the metadata types this field applies to.
    /// </summary>
    public IReadOnlyList<MetadataType> ApplicableMetadataTypes { get; init; } = [];

    /// <summary>
    /// Gets the display order of this field.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Gets a value indicating whether this field is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }
}
