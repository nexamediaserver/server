// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input for updating an admin-defined detail field configuration.
/// </summary>
[GraphQLName("UpdateAdminDetailFieldConfigurationInput")]
public sealed class UpdateAdminDetailFieldConfigurationInput
{
    /// <summary>
    /// Gets or sets the metadata type this configuration targets.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the optional library section identifier when scoping the configuration.
    /// </summary>
    [ID]
    public Guid? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the enabled field types in display order.
    /// </summary>
    public IReadOnlyList<DetailFieldType>? EnabledFieldTypes { get; set; }

    /// <summary>
    /// Gets or sets the disabled field types.
    /// </summary>
    public IReadOnlyList<DetailFieldType>? DisabledFieldTypes { get; set; }

    /// <summary>
    /// Gets or sets the disabled custom field keys.
    /// </summary>
    public IReadOnlyList<string>? DisabledCustomFieldKeys { get; set; }

    /// <summary>
    /// Gets or sets the list of field group definitions.
    /// </summary>
    public IReadOnlyList<DetailFieldGroupInput>? FieldGroups { get; set; }

    /// <summary>
    /// Gets or sets the field-to-group assignments as key-value pairs.
    /// </summary>
    /// <remarks>
    /// Key is the field identifier (either field type name or "Custom:{key}" for custom fields).
    /// Value is the group key.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? FieldGroupAssignments { get; set; }
}
