// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader for fetching metadata items by their IDs.
/// </summary>
public class MetadataItemByIdDataLoader : BatchDataLoader<int, MetadataItem>
{
    private readonly IMetadataItemService metadataItemService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemByIdDataLoader"/> class.
    /// </summary>
    /// <param name="metadataItemService">The metadata item service.</param>
    /// <param name="batchScheduler">The batch scheduler.</param>
    /// <param name="options">Optional data loader options.</param>
    public MetadataItemByIdDataLoader(
        IMetadataItemService metadataItemService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null
    )
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        this.metadataItemService = metadataItemService;
    }

    /// <inheritdoc/>
    protected override async Task<IReadOnlyDictionary<int, MetadataItem>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken
    )
    {
        return await this
            .metadataItemService.GetQueryable()
            .Where(m => keys.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
    }
}
