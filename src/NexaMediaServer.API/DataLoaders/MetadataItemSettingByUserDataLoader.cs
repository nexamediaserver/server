// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Loads <see cref="MetadataItemSetting"/> instances for the current user keyed by metadata item ID.
/// </summary>
public sealed class MetadataItemSettingByUserDataLoader
    : DataLoaderBase<int, MetadataItemSetting?>,
        IMetadataItemSettingByUserDataLoader
{
    private readonly IDbContextFactory<MediaServerContext> contextFactory;
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemSettingByUserDataLoader"/> class.
    /// </summary>
    /// <param name="contextFactory">Factory for creating <see cref="MediaServerContext"/> instances.</param>
    /// <param name="httpContextAccessor">Accessor used to resolve the current user.</param>
    /// <param name="batchScheduler">Scheduler controlling batch execution.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public MetadataItemSettingByUserDataLoader(
        IDbContextFactory<MediaServerContext> contextFactory,
        IHttpContextAccessor httpContextAccessor,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options
    )
        : base(batchScheduler, options ?? throw new ArgumentNullException(nameof(options)))
    {
        this.contextFactory =
            contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.httpContextAccessor =
            httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    protected override async ValueTask FetchAsync(
        IReadOnlyList<int> keys,
        Memory<Result<MetadataItemSetting?>> results,
        DataLoaderFetchContext<MetadataItemSetting?> context,
        CancellationToken cancellationToken
    )
    {
        _ = context;
        var preAwaitSpan = results.Span;
        if (keys.Count == 0)
        {
            return;
        }

        var userId = this.httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userId))
        {
            for (var i = 0; i < keys.Count; i++)
            {
                preAwaitSpan[i] = Result<MetadataItemSetting?>.Resolve(null);
            }

            return;
        }

        var distinctKeys = keys.Distinct().ToArray();

        await using var dbContext = await this
            .contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var settingsByMetadataId = await dbContext
            .MetadataItemSettings.AsNoTracking()
            .Where(s => s.UserId == userId && distinctKeys.Contains(s.MetadataItemId))
            .ToDictionaryAsync(s => s.MetadataItemId, cancellationToken)
            .ConfigureAwait(false);

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            span[i] = settingsByMetadataId.TryGetValue(keys[i], out var setting)
                ? Result<MetadataItemSetting?>.Resolve(setting)
                : Result<MetadataItemSetting?>.Resolve(null);
        }
    }
}
