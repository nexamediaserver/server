// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.API.Types;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader that batches child metadata item lookups by parent item UUID.
/// </summary>
public sealed class ChildMetadataItemsByParentIdDataLoader
    : DataLoaderBase<Guid, IReadOnlyList<MetadataItem>>,
        IChildMetadataItemsByParentIdDataLoader
{
    private static readonly IReadOnlyList<MetadataItem> EmptyItems = Array.Empty<MetadataItem>();

    private readonly IMetadataService metadataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildMetadataItemsByParentIdDataLoader"/> class.
    /// </summary>
    /// <param name="metadataService">The metadata service.</param>
    /// <param name="batchScheduler">The batch scheduler.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public ChildMetadataItemsByParentIdDataLoader(
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
        IReadOnlyList<Guid> keys,
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

        var distinctKeys = keys.Distinct().ToArray();

        var mappedItems = await this
            .metadataService.GetQueryable()
            .Include(item => item.LibrarySection)
            .Where(item => item.Parent != null && distinctKeys.Contains(item.Parent.Uuid))
            .OrderBy(item => item.Index)
            .ThenBy(item => item.Title)
            .Select(MetadataMappings.ToApiType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemsByParent = mappedItems
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MetadataItem>)group.ToList());

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            var parentId = keys[i];
            span[i] = itemsByParent.TryGetValue(parentId, out var items)
                ? Result<IReadOnlyList<MetadataItem>?>.Resolve(items)
                : Result<IReadOnlyList<MetadataItem>?>.Resolve(EmptyItems);
        }
    }
}
