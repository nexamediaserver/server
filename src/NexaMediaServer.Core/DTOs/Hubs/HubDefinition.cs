// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// Represents the definition of a hub, including its type, display properties, and optional filters.
/// </summary>
/// <param name="HubType">The type of hub, determining the query logic.</param>
/// <param name="Title">The display title for the hub.</param>
/// <param name="MetadataType">The metadata type of items in this hub.</param>
/// <param name="Context">The context in which this hub definition applies.</param>
/// <param name="SortOrder">The display order of the hub within its context.</param>
/// <param name="FilterValue">Optional filter value (e.g., genre name, director ID) for filtered hubs.</param>
/// <param name="Widget">The recommended widget type for client-side rendering.</param>
public sealed record HubDefinition(
    HubType HubType,
    string Title,
    MetadataType MetadataType,
    HubContext Context,
    int SortOrder,
    string? FilterValue = null,
    HubWidgetType Widget = HubWidgetType.Slider
);
