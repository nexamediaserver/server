// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// Input for updating a hub configuration for a given scope.
/// </summary>
[GraphQLName("UpdateHubConfigurationInput")]
public sealed class UpdateHubConfigurationInput
{
    /// <summary>
    /// Gets or sets the hub context to update.
    /// </summary>
    public HubContext Context { get; set; }

    /// <summary>
    /// Gets or sets the optional library section identifier for discover/detail scopes.
    /// </summary>
    [ID]
    public Guid? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the optional metadata type for item detail scopes.
    /// </summary>
    public MetadataType? MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the enabled hub types in display order.
    /// </summary>
    public IReadOnlyList<HubType>? EnabledHubTypes { get; set; }

    /// <summary>
    /// Gets or sets the disabled hub types.
    /// </summary>
    public IReadOnlyList<HubType>? DisabledHubTypes { get; set; }
}
