// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.API.Types;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader that batches root metadata item lookups per library section.
/// </summary>
public sealed class RootMetadataItemsBySectionIdDataLoader
    : DataLoaderBase<RootMetadataItemsRequest, IReadOnlyList<MetadataItem>>,
        IRootMetadataItemsBySectionIdDataLoader
{
    private static readonly IReadOnlyList<MetadataItem> EmptyItems = Array.Empty<MetadataItem>();

    private readonly IMetadataService metadataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootMetadataItemsBySectionIdDataLoader"/> class.
    /// </summary>
    /// <param name="metadataService">The metadata service.</param>
    /// <param name="batchScheduler">The batch scheduler.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public RootMetadataItemsBySectionIdDataLoader(
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
        IReadOnlyList<RootMetadataItemsRequest> keys,
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
        var sectionIds = distinctKeys.Select(key => key.SectionId).Distinct().ToArray();
        var filteredTypes = distinctKeys.Select(key => key.MetadataType).Distinct().ToArray();

        // Determine if any of the requested types are "browsable child" types
        // (types that should be returned even when they have a parent, e.g., albums in music libraries).
        var browsableChildTypes = GetBrowsableChildTypes(filteredTypes);
        var rootOnlyTypes = filteredTypes.Except(browsableChildTypes).ToArray();

        var query = this
            .metadataService.GetQueryable()
            .Include(item => item.LibrarySection)
            .Where(item => sectionIds.Contains(item.LibrarySection.Uuid));

        if (filteredTypes is { Length: > 0 })
        {
            // Include items that are either:
            // 1. Root items (ParentId == null) of the requested types, OR
            // 2. Items of browsable child types (regardless of parent)
            query = query.Where(item =>
                (item.ParentId == null && rootOnlyTypes.Contains(item.MetadataType))
                || browsableChildTypes.Contains(item.MetadataType)
            );
        }
        else
        {
            query = query.Where(item => item.ParentId == null);
        }

        var mappedItems = await query
            .Select(MetadataMappings.ToApiType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemsBySection = mappedItems
            .GroupBy(item => item.LibrarySectionUuid)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MetadataItem>)group.ToList());

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            var request = keys[i];
            span[i] = itemsBySection.TryGetValue(request.SectionId, out var items)
                ? Result<IReadOnlyList<MetadataItem>?>.Resolve(
                    ApplyTypeFilter(items, request.MetadataType)
                )
                : Result<IReadOnlyList<MetadataItem>?>.Resolve(EmptyItems);
        }
    }

    private static IReadOnlyList<MetadataItem> ApplyTypeFilter(
        IReadOnlyList<MetadataItem> items,
        MetadataType metadataType
    )
    {
        if (items.Count == 0)
        {
            return EmptyItems;
        }

        var filteredItems = items.Where(item => item.MetadataType == metadataType).ToList();

        return filteredItems.Count == 0 ? EmptyItems : filteredItems;
    }

    /// <summary>
    /// Returns the subset of requested types that should be browsable at the library level
    /// even when they have a parent (e.g., albums under artists in music libraries).
    /// </summary>
    private static MetadataType[] GetBrowsableChildTypes(MetadataType[] requestedTypes)
    {
        // These types are browsable at the library level regardless of having a parent.
        // AlbumRelease: Albums in music libraries (children of artist groups).
        // AlbumReleaseGroup: Album release groups in music libraries.
        var browsableChildTypes = new HashSet<MetadataType>
        {
            MetadataType.AlbumRelease,
            MetadataType.AlbumReleaseGroup,
        };

        return requestedTypes.Where(browsableChildTypes.Contains).ToArray();
    }
}
