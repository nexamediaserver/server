// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// GraphQL representation of a detail field configuration.
/// </summary>
[GraphQLName("DetailFieldConfiguration")]
public sealed class DetailFieldConfigurationType
{
    /// <summary>
    /// Gets or sets the metadata type this configuration targets.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the optional library section identifier when scoped.
    /// </summary>
    [ID]
    public Guid? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of enabled fields.
    /// </summary>
    public IReadOnlyList<DetailFieldType> EnabledFieldTypes { get; set; } = Array.Empty<DetailFieldType>();

    /// <summary>
    /// Gets or sets the list of disabled field types.
    /// </summary>
    public IReadOnlyList<DetailFieldType> DisabledFieldTypes { get; set; } = Array.Empty<DetailFieldType>();

    /// <summary>
    /// Gets or sets the list of disabled custom field keys.
    /// </summary>
    public IReadOnlyList<string> DisabledCustomFieldKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of field group definitions.
    /// </summary>
    public IReadOnlyList<DetailFieldGroupType>? FieldGroups { get; set; }

    /// <summary>
    /// Gets or sets the field-to-group assignments as key-value pairs.
    /// </summary>
    /// <remarks>
    /// Key is the field identifier (either field type name or "Custom:{key}" for custom fields).
    /// Value is the group key.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? FieldGroupAssignments { get; set; }
}
