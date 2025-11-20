// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISessionRepository"/>.
/// </summary>
public sealed class SessionRepository : ISessionRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public SessionRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public async Task<Session> CreateAsync(
        Session session,
        CancellationToken cancellationToken = default
    )
    {
        this.context.Sessions.Add(session);
        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <inheritdoc />
    public async Task<Session> UpdateAsync(
        Session session,
        CancellationToken cancellationToken = default
    )
    {
        this.context.Sessions.Update(session);
        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <inheritdoc />
    public Task<Session?> GetByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default
    )
    {
        return this
            .context.Sessions.Include(s => s.Device)
            .FirstOrDefaultAsync(session => session.PublicId == publicId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Session>> GetByUserAsync(
        string userId,
        bool includeRevoked,
        CancellationToken cancellationToken = default
    )
    {
        var query = this
            .context.Sessions.Include(session => session.Device)
            .Where(session => session.UserId == userId);

        if (!includeRevoked)
        {
            query = query.Where(session => session.RevokedAt == null);
        }

        var sessions = await query
            .OrderByDescending(session => session.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessions;
    }
}
