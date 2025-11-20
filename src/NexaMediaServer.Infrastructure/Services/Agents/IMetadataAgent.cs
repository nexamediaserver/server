// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services.Agents;

/// <summary>
/// Base contract for metadata agents (local or remote) that can enrich <see cref="MetadataItem"/>.
/// </summary>
public interface IMetadataAgent
{
    /// <summary>
    /// Gets the unique (human-readable) name of the agent.
    /// </summary>
    string Name { get; }
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
