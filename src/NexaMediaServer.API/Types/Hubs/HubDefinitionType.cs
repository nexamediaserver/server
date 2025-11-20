// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// Represents a hub definition for the GraphQL API.
/// </summary>
[GraphQLName("HubDefinition")]
public sealed class HubDefinitionType
{
    /// <summary>
    /// Gets the unique key identifying this hub definition.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of hub (e.g., RecentlyAdded, TopRated, etc.).
    /// </summary>
    public HubType Type { get; init; }

    /// <summary>
    /// Gets the display title for this hub.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the metadata type of items in this hub.
    /// </summary>
    public MetadataType MetadataType { get; init; }

    /// <summary>
    /// Gets the recommended widget type for client-side rendering.
    /// </summary>
    public HubWidgetType Widget { get; init; }

    /// <summary>
    /// Gets the library section ID this hub is scoped to, if any.
    /// </summary>
    [ID("LibrarySection")]
    public Guid? LibrarySectionId { get; init; }

    /// <summary>
    /// Gets the context ID (e.g., parent item ID for detail page hubs).
    /// </summary>
    [ID("Item")]
    public Guid? ContextId { get; init; }

    /// <summary>
    /// Gets the optional filter value for this hub (e.g., genre name, director name).
    /// </summary>
    public string? FilterValue { get; init; }
}
