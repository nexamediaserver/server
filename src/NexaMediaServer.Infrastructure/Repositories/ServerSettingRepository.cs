// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for managing server settings using direct database reads.
/// </summary>
public class ServerSettingRepository : IServerSettingRepository
{
    private readonly IDbContextFactory<MediaServerContext> contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSettingRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public ServerSettingRepository(IDbContextFactory<MediaServerContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<ServerSetting?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ServerSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServerSetting>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ServerSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.ServerSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            context.ServerSettings.Add(new ServerSetting
            {
                Key = key,
                Value = value,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var setting = await context.ServerSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting is null)
        {
            return false;
        }

        context.ServerSettings.Remove(setting);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
