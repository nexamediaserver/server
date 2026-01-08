// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Fields;

/// <summary>
/// Represents a group of related fields on an item detail page.
/// </summary>
/// <param name="GroupKey">A unique identifier for the group.</param>
/// <param name="Label">The display label for the group.</param>
/// <param name="LayoutType">The layout style for rendering fields within the group.</param>
/// <param name="SortOrder">The display order of the group relative to other groups.</param>
/// <param name="IsCollapsible">Whether the group can be collapsed by the user.</param>
public sealed record DetailFieldGroup(
    string GroupKey,
    string Label,
    DetailFieldGroupLayoutType LayoutType,
    int SortOrder,
    bool IsCollapsible = false
);
