// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// GraphQL representation of a hub configuration.
/// </summary>
[GraphQLName("HubConfiguration")]
public sealed class HubConfigurationType
{
    /// <summary>
    /// Gets or sets the ordered list of enabled hub types.
    /// </summary>
    public IReadOnlyList<HubType> EnabledHubTypes { get; set; } = Array.Empty<HubType>();

    /// <summary>
    /// Gets or sets the list of disabled hub types.
    /// </summary>
    public IReadOnlyList<HubType> DisabledHubTypes { get; set; } = Array.Empty<HubType>();
}
