// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Fields;

/// <summary>
/// User configuration for field visibility in a specific metadata type context.
/// </summary>
/// <param name="EnabledFieldTypes">The list of enabled field types in display order.</param>
/// <param name="DisabledFieldTypes">The list of explicitly disabled field types.</param>
/// <param name="DisabledCustomFieldKeys">The list of disabled custom field keys.</param>
/// <param name="FieldGroups">The list of field group definitions.</param>
/// <param name="FieldGroupAssignments">Map of field keys to group keys.</param>
public sealed record DetailFieldConfiguration(
    IReadOnlyList<DetailFieldType> EnabledFieldTypes,
    IReadOnlyList<DetailFieldType> DisabledFieldTypes,
    IReadOnlyList<string> DisabledCustomFieldKeys,
    IReadOnlyList<DetailFieldGroup>? FieldGroups = null,
    IReadOnlyDictionary<string, string>? FieldGroupAssignments = null
);
