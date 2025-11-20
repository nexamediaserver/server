// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// Represents the result of fetching hub content, containing items or people based on content type.
/// </summary>
/// <param name="Definition">The hub definition.</param>
/// <param name="Items">The metadata items for this hub (if ContentType is Items).</param>
/// <param name="People">The cast/crew members for this hub (if ContentType is People).</param>
public sealed record HubContent(
    HubDefinition Definition,
    IReadOnlyList<HubItem>? Items = null,
    IReadOnlyList<HubPerson>? People = null
);
