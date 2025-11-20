// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository contract for persisted user sessions.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new session row.
    /// </summary>
    /// <param name="session">Session entity to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted entity.</returns>
    Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists updates to an existing session.
    /// </summary>
    /// <param name="session">Session entity with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    Task<Session> UpdateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by its public identifier.
    /// </summary>
    /// <param name="publicId">Public GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session when found; otherwise null.</returns>
    Task<Session?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a user; optionally include revoked sessions.
    /// </summary>
    /// <param name="userId">Identity user identifier.</param>
    /// <param name="includeRevoked">Include revoked/expired sessions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of sessions.</returns>
    Task<IReadOnlyList<Session>> GetByUserAsync(
        string userId,
        bool includeRevoked,
        CancellationToken cancellationToken = default
    );
}
