// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// Represents a person (cast/crew member) within a hub.
/// </summary>
/// <param name="PersonId">The UUID of the person's metadata item.</param>
/// <param name="Name">The person's name.</param>
/// <param name="Role">The role/character name (for cast) or job title (for crew).</param>
/// <param name="ThumbUrl">The person's thumbnail URL.</param>
/// <param name="Relationship">The relationship type (e.g., Actor, Director).</param>
public sealed record HubPerson(
    Guid PersonId,
    string Name,
    string? Role,
    string? ThumbUrl,
    string Relationship
);
