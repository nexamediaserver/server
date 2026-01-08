// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// Input describing the scope of a hub configuration lookup.
/// </summary>
[GraphQLName("HubConfigurationScopeInput")]
public sealed class HubConfigurationScopeInput
{
    /// <summary>
    /// Gets or sets the hub context being configured.
    /// </summary>
    public HubContext Context { get; set; }

    /// <summary>
    /// Gets or sets the optional library section identifier for discover/detail scopes.
    /// </summary>
    [ID]
    public Guid? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the optional metadata type for item detail configurations.
    /// </summary>
    public MetadataType? MetadataType { get; set; }
}
