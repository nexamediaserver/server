// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// Input for fetching hub items.
/// </summary>
public sealed class GetHubItemsInput
{
    /// <summary>
    /// Gets the type of hub.
    /// </summary>
    public HubType HubType { get; init; }

    /// <summary>
    /// Gets the hub context.
    /// </summary>
    public HubContext Context { get; init; }

    /// <summary>
    /// Gets the optional library section ID for library-specific hubs.
    /// </summary>
    [ID("LibrarySection")]
    public Guid? LibrarySectionId { get; init; }

    /// <summary>
    /// Gets the optional metadata item ID for detail page hubs.
    /// </summary>
    [ID("Item")]
    public Guid? MetadataItemId { get; init; }

    /// <summary>
    /// Gets the optional filter value for filtered hubs.
    /// </summary>
    public string? FilterValue { get; init; }
}
