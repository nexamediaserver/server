// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents a tag for GraphQL API.
/// </summary>
[Node]
[GraphQLName("Tag")]
public class Tag
{
    /// <summary>
    /// Gets the unique identifier for the tag.
    /// </summary>
    [ID]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the tag name.
    /// </summary>
    public string Name { get; init; } = null!;
}
