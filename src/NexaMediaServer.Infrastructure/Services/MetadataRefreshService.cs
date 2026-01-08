// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Enqueues metadata-only refresh jobs for items and libraries.
/// </summary>
public sealed partial class MetadataRefreshService : IMetadataRefreshService
{
    /// <summary>
    /// Collection MetadataTypes that should not have descendants refreshed.
    /// These types aggregate thumbnails from children, so refreshing should only
    /// regenerate the collection image, not refresh all child metadata.
    /// </summary>
    private static readonly FrozenSet<MetadataType> CollectionTypes = new HashSet<MetadataType>
    {
        MetadataType.PhotoAlbum,
        MetadataType.PictureSet,
        MetadataType.BookSeries,
        MetadataType.GameFranchise,
        MetadataType.GameSeries,
        MetadataType.Collection,
        MetadataType.Playlist,
    }.ToFrozenSet();

    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly ILibrarySectionRepository librarySectionRepository;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<MetadataRefreshService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataRefreshService"/> class.
    /// </summary>
    /// <param name="metadataItemRepository">Repository for querying metadata items.</param>
    /// <param name="librarySectionRepository">Repository for library section lookups.</param>
    /// <param name="jobClient">Hangfire client used to enqueue metadata jobs.</param>
    /// <param name="logger">Typed logger for refresh diagnostics.</param>
    public MetadataRefreshService(
        IMetadataItemRepository metadataItemRepository,
        ILibrarySectionRepository librarySectionRepository,
        IBackgroundJobClient jobClient,
        ILogger<MetadataRefreshService> logger
    )
    {
        this.metadataItemRepository = metadataItemRepository;
        this.librarySectionRepository = librarySectionRepository;
        this.jobClient = jobClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<MetadataRefreshResult> EnqueueItemRefreshAsync(
        Guid itemId,
        bool includeDescendants,
        CancellationToken cancellationToken = default
    )
    {
        if (itemId == Guid.Empty)
        {
            return MetadataRefreshResult.Failure("Metadata item id is required.");
        }

        var root = await this
            .metadataItemRepository.GetQueryable()
            .Where(mi => mi.Uuid == itemId)
            .Select(mi => new { mi.Id, mi.Uuid, mi.MetadataType })
            .FirstOrDefaultAsync(cancellationToken);

        if (root is null)
        {
            return MetadataRefreshResult.Failure("Metadata item not found.");
        }

        // Collection types (PhotoAlbum, Playlist, etc.) should not refresh children.
        // They aggregate thumbnails from children, so refreshing should only
        // regenerate the collection image, not refresh all child metadata.
        var isCollectionType = CollectionTypes.Contains(root.MetadataType);
        var shouldIncludeDescendants = includeDescendants && !isCollectionType;

        var targets = new List<Guid> { root.Uuid };

        if (shouldIncludeDescendants)
        {
            var descendants = await this.GetDescendantUuidsAsync(root.Id, cancellationToken);
            targets.AddRange(descendants);
        }

        int enqueued = this.EnqueueMetadataJobs(targets, isCollectionType);
        LogItemRefreshEnqueued(this.logger, enqueued, shouldIncludeDescendants);

        return MetadataRefreshResult.SuccessResult(enqueued);
    }

    /// <inheritdoc />
    public async Task<MetadataRefreshResult> EnqueueLibraryRefreshAsync(
        Guid librarySectionId,
        CancellationToken cancellationToken = default
    )
    {
        if (librarySectionId == Guid.Empty)
        {
            return MetadataRefreshResult.Failure("Library section id is required.");
        }

        var library = await this.librarySectionRepository.GetByUuidAsync(librarySectionId);
        if (library is null)
        {
            return MetadataRefreshResult.Failure("Library section not found.");
        }

        var itemUuids = await this
            .metadataItemRepository.GetQueryable()
            .Where(mi => mi.LibrarySectionId == library.Id)
            .Select(mi => mi.Uuid)
            .ToListAsync(cancellationToken);

        int enqueued = this.EnqueueMetadataJobs(itemUuids);
        LogLibraryRefreshEnqueued(this.logger, librarySectionId, enqueued);

        return MetadataRefreshResult.SuccessResult(enqueued);
    }

    [LoggerMessage(
        EventId = 8301,
        Level = LogLevel.Information,
        Message = "Enqueued metadata refresh for {Count} item(s) (includeDescendants={IncludeDescendants})"
    )]
    private static partial void LogItemRefreshEnqueued(
        ILogger logger,
        int count,
        bool includeDescendants
    );

    [LoggerMessage(
        EventId = 8302,
        Level = LogLevel.Information,
        Message = "Enqueued metadata refresh for library {LibraryId} with {Count} item(s)"
    )]
    private static partial void LogLibraryRefreshEnqueued(
        ILogger logger,
        Guid libraryId,
        int count
    );

    private async Task<List<Guid>> GetDescendantUuidsAsync(
        int rootId,
        CancellationToken cancellationToken
    )
    {
        var collected = new List<Guid>();
        var frontier = new List<int> { rootId };

        while (frontier.Count > 0)
        {
            var children = await this
                .metadataItemRepository.GetQueryable()
                .Where(mi => mi.ParentId != null && frontier.Contains(mi.ParentId.Value))
                .Select(mi => new { mi.Id, mi.Uuid })
                .ToListAsync(cancellationToken);

            if (children.Count == 0)
            {
                break;
            }

            collected.AddRange(children.Select(child => child.Uuid));
            frontier = children.Select(child => child.Id).ToList();
        }

        return collected;
    }

    private int EnqueueMetadataJobs(IEnumerable<Guid> itemUuids, bool isCollectionRefresh = false)
    {
        var distinct = new HashSet<Guid>(itemUuids);
        foreach (var uuid in distinct)
        {
            if (isCollectionRefresh)
            {
                // For collection types, only regenerate the collection image.
                // Skip metadata agents and file analysis since collection metadata
                // is derived from children, not external sources.
                this.jobClient.Enqueue<ICollectionImageOrchestrator>(svc =>
                    svc.RegenerateCollectionImageAsync(uuid, CancellationToken.None));
            }
            else
            {
                // Pass skipAnalysis: true for metadata-only refresh (ISidecarParser, IMetadataAgent, IImageProvider)
                // without triggering file analysis or trickplay generation.
                this.jobClient.Enqueue<MetadataService>(svc => svc.RefreshMetadataAsync(uuid, true));

                // Enqueue image generation jobs to regenerate thumbnails from IImageProvider implementations.
                this.jobClient.Enqueue<MetadataService>(svc => svc.GenerateImagesAsync(uuid));
            }
        }

        return distinct.Count;
    }
}
