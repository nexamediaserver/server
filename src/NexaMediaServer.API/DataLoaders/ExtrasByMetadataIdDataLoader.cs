// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.API.Types;
using NexaMediaServer.Infrastructure.Services;
using CoreMetadataItem = NexaMediaServer.Core.Entities.MetadataItem;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader that batches extras lookups per owning metadata item.
/// </summary>
public sealed class ExtrasByMetadataIdDataLoader
    : DataLoaderBase<int, IReadOnlyList<MetadataItem>>,
        IExtrasByMetadataIdDataLoader
{
    private static readonly IReadOnlyList<MetadataItem> EmptyExtras = Array.Empty<MetadataItem>();
    private static readonly Func<CoreMetadataItem, MetadataItem> MapToApi =
        MetadataMappings.ToApiType.Compile();

    private readonly IMetadataService metadataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtrasByMetadataIdDataLoader"/> class.
    /// </summary>
    /// <param name="metadataService">Metadata service used to query extras.</param>
    /// <param name="batchScheduler">Batch scheduler provided by HotChocolate.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public ExtrasByMetadataIdDataLoader(
        IMetadataService metadataService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options
    )
        : base(batchScheduler, options ?? throw new ArgumentNullException(nameof(options)))
    {
        this.metadataService =
            metadataService ?? throw new ArgumentNullException(nameof(metadataService));
    }

    /// <inheritdoc />
    protected override async ValueTask FetchAsync(
        IReadOnlyList<int> keys,
        Memory<Result<IReadOnlyList<MetadataItem>?>> results,
        DataLoaderFetchContext<IReadOnlyList<MetadataItem>> context,
        CancellationToken cancellationToken
    )
    {
        _ = context;
        if (keys.Count == 0)
        {
            return;
        }

        var distinctOwners = keys.Distinct().ToArray();
        var extrasByOwner = await this
            .metadataService.GetExtrasByOwnersAsync(distinctOwners, cancellationToken)
            .ConfigureAwait(false);

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            var ownerId = keys[i];
            if (!extrasByOwner.TryGetValue(ownerId, out var extras) || extras.Count == 0)
            {
                span[i] = Result<IReadOnlyList<MetadataItem>?>.Resolve(EmptyExtras);
                continue;
            }

            var mapped = extras.Select(MapToApi).ToList();
            span[i] = Result<IReadOnlyList<MetadataItem>?>.Resolve(mapped);
        }
    }
}
