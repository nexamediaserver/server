// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.DTOs.Authentication;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services.Authentication;

/// <summary>
/// Application service responsible for managing revokable user sessions.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Ensures the specified device exists (upserting as needed) for the given user.
    /// </summary>
    /// <param name="userId">The user identifier that owns the device.</param>
    /// <param name="registration">Device registration payload supplied by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created device.</returns>
    Task<Device> UpsertDeviceAsync(
        string userId,
        DeviceRegistration registration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a new session linked to the provided device.
    /// </summary>
    /// <param name="request">Session creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted session record.</returns>
    Task<Session> CreateSessionAsync(
        SessionCreationRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Looks up a session by public identifier, returning null when revoked or expired.
    /// </summary>
    /// <param name="publicId">Public session identifier embedded in JWTs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session or null if not valid.</returns>
    Task<Session?> ValidateSessionAsync(
        Guid publicId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Revokes the session identified by the supplied public id.
    /// </summary>
    /// <param name="publicId">Public session identifier to revoke.</param>
    /// <param name="reason">Optional reason for auditing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when a session was revoked; otherwise false.</returns>
    Task<bool> RevokeSessionAsync(
        Guid publicId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets summaries of sessions for the given user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="includeRevoked">When true, include revoked sessions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of session summaries.</returns>
    Task<IReadOnlyList<SessionSummary>> GetSessionsForUserAsync(
        string userId,
        bool includeRevoked,
        CancellationToken cancellationToken = default
    );
}
