// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents user-specific field visibility configuration for a specific metadata type.
/// </summary>
public class UserDetailFieldConfiguration : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the user this configuration belongs to.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user this configuration belongs to.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the metadata type this configuration applies to.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled field types in display order.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON array of field type enum values.
    /// When non-empty, only these field types are shown in the specified order.
    /// </remarks>
    public List<DetailFieldType> EnabledFieldTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of explicitly disabled field types.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON array of field type enum values.
    /// These field types are hidden regardless of default configuration.
    /// </remarks>
    public List<DetailFieldType> DisabledFieldTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of disabled custom field keys.
    /// </summary>
    /// <remarks>
    /// Stored as a JSON array of custom field keys.
    /// Allows users to hide specific custom fields by their key.
    /// </remarks>
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
