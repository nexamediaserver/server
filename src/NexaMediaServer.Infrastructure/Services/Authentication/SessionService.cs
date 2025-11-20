// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.DTOs.Authentication;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services.Authentication;

namespace NexaMediaServer.Infrastructure.Services.Authentication;

/// <summary>
/// Default implementation of <see cref="ISessionService"/>.
/// </summary>
public sealed partial class SessionService : ISessionService
{
    private static readonly ConcurrentDictionary<Guid, CachedSession> SessionCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IDeviceRepository deviceRepository;
    private readonly ISessionRepository sessionRepository;
    private readonly SessionOptions options;
    private readonly ILogger<SessionService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class.
    /// </summary>
    /// <param name="deviceRepository">Repository used to persist devices.</param>
    /// <param name="sessionRepository">Repository used to persist sessions.</param>
    /// <param name="options">Session configuration options.</param>
    /// <param name="logger">Application logger.</param>
    public SessionService(
        IDeviceRepository deviceRepository,
        ISessionRepository sessionRepository,
        IOptions<SessionOptions> options,
        ILogger<SessionService> logger
    )
    {
        this.deviceRepository = deviceRepository;
        this.sessionRepository = sessionRepository;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<Device> UpsertDeviceAsync(
        string userId,
        DeviceRegistration registration,
        CancellationToken cancellationToken = default
    )
    {
        var device = await this
            .deviceRepository.GetByIdentifierAsync(
                userId,
                registration.Identifier,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (device is null)
        {
            device = new Device
            {
                UserId = userId,
                Identifier = registration.Identifier,
                Name = registration.Name,
                Platform = registration.Platform,
                Version = registration.Version,
            };

            await this
                .deviceRepository.CreateAsync(device, cancellationToken)
                .ConfigureAwait(false);
            return device;
        }

        var hasChanges = false;
        if (!string.Equals(device.Name, registration.Name, StringComparison.Ordinal))
        {
            device.Name = registration.Name;
            hasChanges = true;
        }

        if (!string.Equals(device.Platform, registration.Platform, StringComparison.Ordinal))
        {
            device.Platform = registration.Platform;
            hasChanges = true;
        }

        if (!string.Equals(device.Version, registration.Version, StringComparison.Ordinal))
        {
            device.Version = registration.Version;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await this
                .deviceRepository.UpdateAsync(device, cancellationToken)
                .ConfigureAwait(false);
        }

        return device;
    }

    /// <inheritdoc />
    public async Task<Session> CreateSessionAsync(
        SessionCreationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = DateTime.UtcNow;
        var expiresAt = utcNow.AddDays(Math.Max(1, this.options.LifetimeDays));

        var session = new Session
        {
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            PublicId = Guid.NewGuid(),
            ClientVersion = request.ClientVersion,
            LastUsedAt = utcNow,
            ExpiresAt = expiresAt,
        };

        return await this
            .sessionRepository.CreateAsync(session, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Session?> ValidateSessionAsync(
        Guid publicId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        // Check cache first for fast path
        if (SessionCache.TryGetValue(publicId, out var cached) && cached.ValidUntil > now)
        {
            return cached.Session;
        }

        var session = await this
            .sessionRepository.GetByPublicIdAsync(publicId, cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            // Cache negative result briefly to avoid repeated lookups
            SessionCache[publicId] = new CachedSession(null, now.AddSeconds(5));
            return null;
        }

        if (session.RevokedAt is not null)
        {
            Log.SessionRevoked(this.logger, publicId);
            SessionCache.TryRemove(publicId, out _);
            return null;
        }

        if (session.ExpiresAt <= now)
        {
            Log.SessionExpired(this.logger, publicId, session.ExpiresAt);
            SessionCache.TryRemove(publicId, out _);
            return null;
        }

        // Update LastUsedAt lazily - only if not updated recently
        if (session.LastUsedAt is null || (now - session.LastUsedAt.Value).TotalMinutes >= 1)
        {
            session.LastUsedAt = now;
            await this
                .sessionRepository.UpdateAsync(session, cancellationToken)
                .ConfigureAwait(false);
        }

        // Cache valid session
        SessionCache[publicId] = new CachedSession(session, now.Add(CacheDuration));
        return session;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(
        Guid publicId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        var session = await this
            .sessionRepository.GetByPublicIdAsync(publicId, cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.RevokedAt is not null)
        {
            return false;
        }

        session.RevokedAt = DateTime.UtcNow;
        session.RevokedReason = reason;
        await this.sessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionSummary>> GetSessionsForUserAsync(
        string userId,
        bool includeRevoked,
        CancellationToken cancellationToken = default
    )
    {
        var sessions = await this
            .sessionRepository.GetByUserAsync(userId, includeRevoked, cancellationToken)
            .ConfigureAwait(false);
        return sessions
            .Select(session => new SessionSummary(
                session.PublicId,
                session.Device.Name,
                session.Device.Platform,
                session.ExpiresAt,
                session.LastUsedAt,
                session.RevokedAt is not null
            ))
            .ToList();
    }

    private sealed record CachedSession(Session? Session, DateTime ValidUntil);

    /// <summary>
    /// Logging helpers for <see cref="SessionService"/>.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Session {SessionId} is revoked"
        )]
        public static partial void SessionRevoked(ILogger logger, Guid sessionId);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Debug,
            Message = "Session {SessionId} expired at {ExpiresAt}"
        )]
        public static partial void SessionExpired(
            ILogger logger,
            Guid sessionId,
            DateTime expiresAt
        );
    }
}
