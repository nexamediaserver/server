// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents an admin-defined field layout configuration for a metadata type, optionally scoped to a library section.
/// </summary>
public sealed class DetailFieldConfigurationOverride : AuditableEntity
{
    /// <summary>
    /// Gets or sets the metadata type this configuration applies to.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the optional library section ID this configuration is scoped to.
    /// When null, the configuration applies to all libraries.
    /// </summary>
    public int? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library section navigation property.
    /// </summary>
    public LibrarySection? LibrarySection { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled field types in display order.
    /// </summary>
    public List<DetailFieldType> EnabledFieldTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of explicitly disabled field types.
    /// </summary>
    public List<DetailFieldType> DisabledFieldTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of disabled custom field keys.
    /// </summary>
    public List<string> DisabledCustomFieldKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets the field group definitions as JSON.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON array of DetailFieldGroup objects.
    /// Defines groups of fields with their layout types and display order.
    /// </remarks>
    public string? FieldGroupsJson { get; set; }

    /// <summary>
    /// Gets or sets the field-to-group assignments as JSON.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON object mapping field keys to group keys.
    /// Determines which group each field belongs to.
    /// </remarks>
    public string? FieldGroupAssignmentsJson { get; set; }
}
