// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents an available root item type option for browsing a library section.
/// </summary>
[GraphQLName("BrowsableItemType")]
public sealed class BrowsableItemType
{
    /// <summary>
    /// Gets the user-facing display name for this item type.
    /// </summary>
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// Gets the metadata types that this option represents.
    /// When multiple types are present (e.g., Person and Group for Artists),
    /// items of any of these types will be included.
    /// </summary>
    public List<MetadataType> MetadataTypes { get; init; } = [];
}
