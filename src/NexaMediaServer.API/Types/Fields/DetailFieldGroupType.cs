// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Represents a field group definition for the GraphQL API.
/// </summary>
[GraphQLName("DetailFieldGroup")]
public sealed class DetailFieldGroupType
{
    /// <summary>
    /// Gets the unique key identifying this group.
    /// </summary>
    public string GroupKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display label for this group.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the layout type for rendering fields within this group.
    /// </summary>
    public DetailFieldGroupLayoutType LayoutType { get; init; }

    /// <summary>
    /// Gets the display order of this group.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Gets a value indicating whether this group can be collapsed by the user.
    /// </summary>
    public bool IsCollapsible { get; init; }
}
