// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Loads <see cref="MetadataItemSetting"/> instances for the current user keyed by metadata item ID.
/// </summary>
public sealed class MetadataItemSettingByUserDataLoader : BatchDataLoader<int, MetadataItemSetting?>
{
    private readonly IDbContextFactory<MediaServerContext> contextFactory;
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemSettingByUserDataLoader"/> class.
    /// </summary>
    /// <param name="contextFactory">Factory for creating <see cref="MediaServerContext"/> instances.</param>
    /// <param name="httpContextAccessor">Accessor used to resolve the current user.</param>
    /// <param name="batchScheduler">Scheduler controlling batch execution.</param>
    /// <param name="options">Optional data loader configuration.</param>
    public MetadataItemSettingByUserDataLoader(
        IDbContextFactory<MediaServerContext> contextFactory,
        IHttpContextAccessor httpContextAccessor,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null
    )
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        this.contextFactory = contextFactory;
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<int, MetadataItemSetting?>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken
    )
    {
        var result = keys.Distinct().ToDictionary(k => k, _ => (MetadataItemSetting?)null);
        var userId = this.httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userId))
        {
            return result;
        }

        await using var dbContext = await this.contextFactory.CreateDbContextAsync(
            cancellationToken
        );

        var settings = await dbContext
            .MetadataItemSettings.Where(s => s.UserId == userId && keys.Contains(s.MetadataItemId))
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            result[setting.MetadataItemId] = setting;
        }

        return result;
    }
}
