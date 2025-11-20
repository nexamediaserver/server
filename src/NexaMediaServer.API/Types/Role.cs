// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents a cast role linking a person to a metadata item.
/// </summary>
[GraphQLName("Role")]
public sealed class Role
{
    /// <summary>
    /// Gets the identifier of the person metadata item.
    /// </summary>
    [ID("Item")]
    public Guid PersonId { get; init; }

    /// <summary>
    /// Gets the display name of the person.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the thumbnail URL for the person when available.
    /// </summary>
    [GraphQLName("thumbUrl")]
    public string? ThumbUrl { get; init; }

    /// <summary>
    /// Gets the relationship text (for example, a character name).
    /// </summary>
    public string? Relationship { get; init; }
}
