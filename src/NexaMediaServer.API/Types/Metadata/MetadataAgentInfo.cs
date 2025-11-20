// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Metadata;

/// <summary>
/// Represents a metadata agent available in the system for GraphQL exposure.
/// </summary>
[GraphQLName("MetadataAgentInfo")]
public sealed class MetadataAgentInfo
{
    /// <summary>
    /// Gets the unique identifier/name of the agent.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the human-readable display name for the UI.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets a user-friendly description of what this agent does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the category of this agent (Sidecar, Embedded, Local, Remote).
    /// </summary>
    public required MetadataAgentCategory Category { get; init; }

    /// <summary>
    /// Gets the default execution order. Lower values run first.
    /// </summary>
    public required int DefaultOrder { get; init; }

    /// <summary>
    /// Gets the library types this agent supports.
    /// Empty means the agent supports all library types.
    /// </summary>
    public required IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; init; }
}
