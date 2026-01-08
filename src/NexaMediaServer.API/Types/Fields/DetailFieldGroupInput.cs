// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input for creating or updating a detail field group.
/// </summary>
[GraphQLName("DetailFieldGroupInput")]
public sealed class DetailFieldGroupInput
{
    /// <summary>
    /// Gets or sets the unique key identifying this group.
    /// </summary>
    public required string GroupKey { get; set; }

    /// <summary>
    /// Gets or sets the display label for this group.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Gets or sets the layout type for rendering fields within this group.
    /// </summary>
    public required DetailFieldGroupLayoutType LayoutType { get; set; }

    /// <summary>
    /// Gets or sets the display order of this group.
    /// </summary>
    public required int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this group can be collapsed by the user.
    /// </summary>
    public bool IsCollapsible { get; set; }
}
