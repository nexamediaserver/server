// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.API.Types;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader for fetching metadata items by their IDs.
/// </summary>
public sealed class MetadataItemByIdDataLoader
    : DataLoaderBase<Guid, MetadataItem>,
        IMetadataItemByIdDataLoader
{
    private readonly IMetadataItemService metadataItemService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemByIdDataLoader"/> class.
    /// </summary>
    /// <param name="metadataItemService">The metadata item service.</param>
    /// <param name="batchScheduler">The batch scheduler.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public MetadataItemByIdDataLoader(
        IMetadataItemService metadataItemService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options
    )
        : base(batchScheduler, options ?? throw new ArgumentNullException(nameof(options)))
    {
        this.metadataItemService =
            metadataItemService ?? throw new ArgumentNullException(nameof(metadataItemService));
    }

    /// <inheritdoc/>
    protected override async ValueTask FetchAsync(
        IReadOnlyList<Guid> keys,
        Memory<Result<MetadataItem?>> results,
        DataLoaderFetchContext<MetadataItem> context,
        CancellationToken cancellationToken
    )
    {
        _ = context;
        if (keys.Count == 0)
        {
            return;
        }

        var distinctKeys = keys.Distinct().ToArray();

        var itemsByUuid = await this
            .metadataItemService.GetQueryable()
            .Include(m => m.LibrarySection)
            .Where(m => distinctKeys.Contains(m.Uuid))
            .Select(MetadataMappings.ToApiType)
            .ToDictionaryAsync(m => m.Id, cancellationToken)
            .ConfigureAwait(false);

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            span[i] = itemsByUuid.TryGetValue(keys[i], out var item)
                ? Result<MetadataItem?>.Resolve(item)
                : Result<MetadataItem?>.Resolve(null);
        }
    }
}
