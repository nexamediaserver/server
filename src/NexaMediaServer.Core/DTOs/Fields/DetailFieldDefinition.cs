// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Fields;

/// <summary>
/// Represents the definition of a field displayed on an item detail page.
/// </summary>
/// <param name="FieldType">The type of field, determining the data source.</param>
/// <param name="Label">The display label for the field.</param>
/// <param name="Widget">The recommended widget type for client-side rendering.</param>
/// <param name="SortOrder">The display order of the field within its context.</param>
/// <param name="CustomFieldKey">
/// The key for custom fields stored in ExtraFields. Only applicable when FieldType is Custom.
/// </param>
/// <param name="GroupKey">
/// The key of the group this field belongs to. If null, the field is ungrouped.
/// </param>
public sealed record DetailFieldDefinition(
    DetailFieldType FieldType,
    string Label,
    DetailFieldWidgetType Widget,
    int SortOrder,
    string? CustomFieldKey = null,
    string? GroupKey = null
);
