// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Agents;

/// <summary>
/// Base contract for metadata agents (local or remote) that can enrich <see cref="MetadataItem"/>.
/// </summary>
public interface IMetadataAgent : IHasOrder
{
    /// <summary>
    /// Gets the unique identifier for this agent to use for logging and artifact naming.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable display name for the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a user-friendly description of what this agent does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the library types this agent supports.
    /// Return an empty collection if the agent supports all library types.
    /// </summary>
    IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; }
}

/// <summary>
/// An agent that derives metadata exclusively from local filesystem / embedded sources.
/// </summary>
public interface ILocalMetadataAgent : IMetadataAgent
{
    /// <summary>
    /// Retrieves metadata for the provided item, honoring per-library settings.
    /// </summary>
    /// <param name="item">The item to enrich.</param>
    /// <param name="library">The owning library section (with settings).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent metadata result or null if this agent yields nothing.</returns>
    Task<AgentMetadataResult?> GetMetadataAsync(
        MetadataItem item,
        LibrarySection library,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// An agent that calls remote web services / APIs to retrieve metadata.
/// </summary>
public interface IRemoteMetadataAgent : IMetadataAgent
{
    /// <summary>
    /// Retrieves metadata for the provided item, honoring per-library settings.
    /// </summary>
    /// <param name="item">The item to enrich.</param>
    /// <param name="library">The owning library section (with settings).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent metadata result or null if this agent yields nothing.</returns>
    Task<AgentMetadataResult?> GetMetadataAsync(
        MetadataItem item,
        LibrarySection library,
        CancellationToken cancellationToken
    );
}
