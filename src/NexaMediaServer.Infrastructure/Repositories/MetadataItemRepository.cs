// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing metadata items.
/// </summary>
public partial class MetadataItemRepository : IMetadataItemRepository
{
    private readonly MediaServerContext context;
    private readonly IMetadataItemUpdateNotifier updateNotifier;
    private readonly ILogger<MetadataItemRepository> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    /// <param name="updateNotifier">Notifier used to publish update events.</param>
    /// <param name="logger">Typed logger for repository diagnostics.</param>
    public MetadataItemRepository(
        MediaServerContext context,
        IMetadataItemUpdateNotifier updateNotifier,
        ILogger<MetadataItemRepository> logger
    )
    {
        this.context = context;
        this.updateNotifier = updateNotifier;
        this.logger = logger;
    }

    /// <inheritdoc />
    public IQueryable<MetadataItem> GetQueryable()
    {
        return this
            .context.MetadataItems.AsNoTracking()
            .OrderBy(m => m.SortTitle)
            .ThenBy(m => m.Id);
    }

    /// <summary>
    /// Gets a tracking queryable for metadata items (used when updating related tracked entities).
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> with change tracking enabled.</returns>
    public IQueryable<MetadataItem> GetTrackedQueryable() => this.context.MetadataItems;

    /// <inheritdoc />
    public Task AddAsync(MetadataItem item)
    {
        this.context.MetadataItems.Add(item);
        return this.context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task BulkInsertAsync(
        IEnumerable<MetadataBaseItem> items,
        CancellationToken cancellationToken = default
    )
    {
        var dtoList = items?.ToList() ?? new List<MetadataBaseItem>();
        if (dtoList.Count == 0)
        {
            return;
        }

        // Flatten the hierarchy: collect all items including nested children
        var flatDtoList = new List<MetadataBaseItem>();
        var parentUuidByChildUuid = new Dictionary<Guid, Guid>();

        foreach (var dto in dtoList)
        {
            MetadataItemRepositoryHelpers.FlattenHierarchy(
                dto,
                flatDtoList,
                parentUuidByChildUuid,
                parentUuid: null
            );
        }

        // Convert DTOs to entities
        var metadataItems = flatDtoList.Select(MetadataItemMapper.MapToEntity).ToList();

        // Clear children on entities to prevent BulkExtensions from trying to traverse
        foreach (var entity in metadataItems)
        {
            entity.Children = [];
            entity.Parent = null;
        }

        var primaryPathByUuid = metadataItems.ToDictionary(
            m => m.Uuid,
            MetadataItemRepositoryHelpers.GetPrimaryMediaPath
        );

        var pendingRelations = MetadataItemRepositoryHelpers.CollectPendingRelations(
            flatDtoList,
            metadataItems
        );

        // SQLite does not support IncludeGraph. Insert in multiple phases.
        // Wrap in a transaction for atomicity across phases.
        await using var transaction = await this.context.Database.BeginTransactionAsync(
            cancellationToken
        );

        // Apply audit timestamps before bulk insert (BulkExtensions bypasses EF change tracker).
        BulkAuditTimestamps.ApplyInsertTimestamps(metadataItems);

        // 1) Insert MetadataItems, then reload Ids by querying back via Uuid
        var metadataCfg = new BulkConfig { PreserveInsertOrder = true, SetOutputIdentity = false };

        await this.context.BulkInsertAsync(
            metadataItems,
            metadataCfg,
            cancellationToken: cancellationToken
        );

        var uuids = metadataItems.Select(m => m.Uuid).ToList();
        var metadataIdMap = await this
            .context.MetadataItems.AsNoTracking()
            .Where(m => uuids.Contains(m.Uuid))
            .Select(m => new { m.Uuid, m.Id })
            .ToDictionaryAsync(x => x.Uuid, x => x.Id, cancellationToken);

        // 1b) Update ParentId for items that have parents
        await this.UpdateParentIdsAsync(
            parentUuidByChildUuid,
            metadataIdMap,
            cancellationToken
        );

        var relationsToInsert = await this.ResolveRelationTargetsAsync(
            pendingRelations,
            metadataIdMap,
            primaryPathByUuid,
            cancellationToken
        );

        // 2) Prepare and insert MediaItems, wiring MetadataItemId from the inserted parents.
        var mediaItems = new List<MediaItem>();
        foreach (var mi in metadataItems)
        {
            var children = mi.MediaItems ?? Enumerable.Empty<MediaItem>();
            foreach (var media in children)
            {
                if (metadataIdMap.TryGetValue(mi.Uuid, out var metaId))
                {
                    media.MetadataItemId = metaId;
                }

                mediaItems.Add(media);
            }

            // Clear navigations to avoid BulkExtensions trying to traverse graphs
            mi.MediaItems = [];
            mi.Parent = null;
            mi.Children = [];
            mi.LibrarySection = null!;
        }

        if (mediaItems.Count > 0)
        {
            // Validate all SectionLocationIds exist and are non-zero to prevent FK failures
            var missingSectionLocations = new List<int>();
            var sectionIds = mediaItems.Select(m => m.SectionLocationId).Distinct().ToList();
            if (sectionIds.Contains(0))
            {
                throw new InvalidOperationException(
                    "One or more MediaItem.SectionLocationId values are 0 (unset). Cannot insert."
                );
            }

            var existingSectionIds = await this
                .context.SectionsLocations.Where(s => sectionIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            missingSectionLocations.AddRange(sectionIds.Except(existingSectionIds));
            if (missingSectionLocations.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Missing SectionLocation rows for IDs: {string.Join(", ", missingSectionLocations.Take(10))}"
                );
            }

            // Clear navigations
            foreach (var media in mediaItems)
            {
                media.MetadataItem = null!;
                media.SectionLocation = null!;
            }

            // Apply audit timestamps before bulk insert (BulkExtensions bypasses EF change tracker).
            BulkAuditTimestamps.ApplyInsertTimestamps(mediaItems);

            var mediaCfg = new BulkConfig { PreserveInsertOrder = true, SetOutputIdentity = false };

            await this.context.BulkInsertAsync(
                mediaItems,
                mediaCfg,
                cancellationToken: cancellationToken
            );

            // Reload MediaItem IDs by (MetadataItemId, SectionLocationId)
            var metaIds = mediaItems.Select(m => m.MetadataItemId).Distinct().ToList();
            var sectionIdsSet = mediaItems.Select(m => m.SectionLocationId).Distinct().ToHashSet();

            var dbMediaItems = await this
                .context.MediaItems.AsNoTracking()
                .Where(m => metaIds.Contains(m.MetadataItemId))
                .Select(m => new
                {
                    m.Id,
                    m.MetadataItemId,
                    m.SectionLocationId,
                })
                .ToListAsync(cancellationToken);

            var mediaIdMap = dbMediaItems
                .Where(m => sectionIdsSet.Contains(m.SectionLocationId))
                .GroupBy(m => new { m.MetadataItemId, m.SectionLocationId })
                .ToDictionary(
                    g => (g.Key.MetadataItemId, g.Key.SectionLocationId),
                    g => g.Max(x => x.Id)
                );

            // 3) Prepare and insert MediaParts, wiring MediaItemId from the inserted media items.
            var mediaParts = new List<MediaPart>();
            foreach (var media in mediaItems)
            {
                var parts = media.Parts ?? Enumerable.Empty<MediaPart>();
                foreach (var part in parts)
                {
                    if (
                        mediaIdMap.TryGetValue(
                            (media.MetadataItemId, media.SectionLocationId),
                            out var mid
                        )
                    )
                    {
                        part.MediaItemId = mid;
                    }

                    mediaParts.Add(part);
                }

                media.Parts = [];
            }

            if (mediaParts.Count > 0)
            {
                foreach (var part in mediaParts)
                {
                    part.MediaItem = null!;
                }

                // Apply audit timestamps before bulk insert (BulkExtensions bypasses EF change tracker).
                BulkAuditTimestamps.ApplyInsertTimestamps(mediaParts);

                var partsCfg = new BulkConfig
                {
                    PreserveInsertOrder = true,
                    SetOutputIdentity = false,
                };

                await this.context.BulkInsertAsync(
                    mediaParts,
                    partsCfg,
                    cancellationToken: cancellationToken
                );
            }
        }

        if (relationsToInsert.Count > 0)
        {
            await this.context.MetadataRelations.AddRangeAsync(
                relationsToInsert,
                cancellationToken
            );
            await this.context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MetadataItem?> GetByUuidAsync(Guid id)
    {
        return await this
            .context.MetadataItems.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Uuid == id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(MetadataItem item)
    {
        this.context.MetadataItems.Update(item);
        await this.context.SaveChangesAsync();
        try
        {
            await this.updateNotifier.NotifyUpdatedAsync(item.Uuid).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort: do not throw if notifications fail.
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IReadOnlyList<MetadataItem>>> GetExtrasByOwnersAsync(
        IReadOnlyCollection<int> ownerMetadataIds,
        CancellationToken cancellationToken = default
    )
    {
        if (ownerMetadataIds == null || ownerMetadataIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<MetadataItem>>();
        }

        var relationTypes = new[]
        {
            RelationType.ClipSupplementsMetadata,
            RelationType.TrailerPromotesMetadata,
        };

        var extras = await this
            .context.MetadataRelations.AsNoTracking()
            .Where(relation => ownerMetadataIds.Contains(relation.RelatedMetadataItemId))
            .Where(relation => relationTypes.Contains(relation.RelationType))
            .Select(relation => new
            {
                relation.RelatedMetadataItemId,
                Extra = relation.MetadataItem,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return extras
            .GroupBy(x => x.RelatedMetadataItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MetadataItem>)group.Select(x => x.Extra).ToList()
            );
    }

    /// <inheritdoc />
    public async Task<
        IReadOnlyDictionary<int, IReadOnlyList<CastMember>>
    > GetCastByMetadataIdsAsync(
        IReadOnlyCollection<int> metadataIds,
        CancellationToken cancellationToken = default
    )
    {
        if (metadataIds == null || metadataIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<CastMember>>();
        }

        var castMembers = await this
            .context.MetadataRelations.AsNoTracking()
            .Where(relation => metadataIds.Contains(relation.RelatedMetadataItemId))
            .Where(relation => relation.RelationType == RelationType.PersonPerformsInVideo)
            .Select(relation => new
            {
                relation.RelatedMetadataItemId,
                relation.RelationType,
                relation.Text,
                Cast = relation.MetadataItem,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return castMembers
            .GroupBy(x => x.RelatedMetadataItemId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<CastMember>)
                        group.Select(x => new CastMember(x.Cast, x.RelationType, x.Text)).ToList()
            );
    }

    private async Task<List<MetadataRelation>> ResolveRelationTargetsAsync(
        List<(
            Guid SourceUuid,
            string OwnerPath,
            RelationType RelationType,
            string? Text
        )> pendingRelations,
        Dictionary<Guid, int> metadataIdMap,
        Dictionary<Guid, string?> primaryPathByUuid,
        CancellationToken cancellationToken
    )
    {
        var relations = new List<MetadataRelation>();
        if (pendingRelations.Count == 0)
        {
            return relations;
        }

        var batchOwnerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in primaryPathByUuid)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value))
            {
                continue;
            }

            if (metadataIdMap.TryGetValue(kvp.Key, out var metaId))
            {
                batchOwnerMap[kvp.Value!] = metaId;
            }
        }

        var unresolved =
            new List<(int SourceId, string OwnerPath, RelationType RelationType, string? Text)>();

        foreach (var pending in pendingRelations)
        {
            if (!metadataIdMap.TryGetValue(pending.SourceUuid, out var sourceId))
            {
                continue;
            }

            if (batchOwnerMap.TryGetValue(pending.OwnerPath, out var targetId))
            {
                relations.Add(
                    new MetadataRelation
                    {
                        MetadataItemId = sourceId,
                        RelatedMetadataItemId = targetId,
                        RelationType = pending.RelationType,
                        Text = pending.Text,
                    }
                );
            }
            else
            {
                unresolved.Add((sourceId, pending.OwnerPath, pending.RelationType, pending.Text));
            }
        }

        if (unresolved.Count == 0)
        {
            return relations;
        }

        var unresolvedPaths = unresolved
            .Select(r => r.OwnerPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dbMatches = await this
            .context.MediaParts.AsNoTracking()
            .Where(mp => unresolvedPaths.Contains(mp.File))
            .Select(mp => new { mp.File, mp.MediaItem.MetadataItemId })
            .ToListAsync(cancellationToken);

        var dbOwnerMap = dbMatches
            .Where(match => match.MetadataItemId != 0)
            .GroupBy(match =>
                MetadataItemRepositoryHelpers.NormalizePath(match.File) ?? string.Empty
            )
            .Where(group => !string.IsNullOrEmpty(group.Key))
            .ToDictionary(
                group => group.Key,
                group => group.First().MetadataItemId,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var pending in unresolved)
        {
            if (dbOwnerMap.TryGetValue(pending.OwnerPath, out var targetId))
            {
                relations.Add(
                    new MetadataRelation
                    {
                        MetadataItemId = pending.SourceId,
                        RelatedMetadataItemId = targetId,
                        RelationType = pending.RelationType,
                        Text = pending.Text,
                    }
                );
            }
            else
            {
                MetadataItemRepositoryLoggerMessages.LogRelationResolutionFailure(
                    this.logger,
                    pending.OwnerPath
                );
            }
        }

        return relations;
    }

    /// <summary>
    /// Updates ParentId for items that have parent relationships.
    /// </summary>
    private async Task UpdateParentIdsAsync(
        Dictionary<Guid, Guid> parentUuidByChildUuid,
        Dictionary<Guid, int> metadataIdMap,
        CancellationToken cancellationToken
    )
    {
        if (parentUuidByChildUuid.Count == 0)
        {
            return;
        }

        var updates = new List<MetadataItem>();

        foreach (var (childUuid, parentUuid) in parentUuidByChildUuid)
        {
            if (
                metadataIdMap.TryGetValue(childUuid, out var childId)
                && metadataIdMap.TryGetValue(parentUuid, out var parentId)
            )
            {
                updates.Add(new MetadataItem { Id = childId, ParentId = parentId });
            }
        }

        if (updates.Count > 0)
        {
            await this.context.BulkUpdateAsync(
                updates,
                new BulkConfig
                {
                    PropertiesToInclude = new List<string> { nameof(MetadataItem.ParentId) },
                    UpdateByProperties = new List<string> { nameof(MetadataItem.Id) },
                },
                cancellationToken: cancellationToken
            );
        }
    }

    private static class MetadataItemRepositoryHelpers
    {
        /// <summary>
        /// Flattens a metadata item hierarchy into a list, tracking parent-child relationships.
        /// </summary>
        internal static void FlattenHierarchy(
            MetadataBaseItem item,
            List<MetadataBaseItem> flatList,
            Dictionary<Guid, Guid> parentUuidByChildUuid,
            Guid? parentUuid
        )
        {
            // Ensure item has a UUID
            if (item.Uuid == Guid.Empty)
            {
                item.Uuid = Guid.NewGuid();
            }

            flatList.Add(item);

            // Track parent relationship
            if (parentUuid.HasValue)
            {
                parentUuidByChildUuid[item.Uuid] = parentUuid.Value;
            }

            // Recursively process children
            foreach (var child in item.Children)
            {
                FlattenHierarchy(child, flatList, parentUuidByChildUuid, item.Uuid);
            }

            // Clear children after flattening to avoid duplication
            item.Children = [];
        }

        internal static List<(
            Guid SourceUuid,
            string OwnerPath,
            RelationType RelationType,
            string? Text
        )> CollectPendingRelations(List<MetadataBaseItem> dtoList, List<MetadataItem> metadataItems)
        {
            var pending = new List<(Guid, string, RelationType, string?)>();

            for (var i = 0; i < dtoList.Count && i < metadataItems.Count; i++)
            {
                if (dtoList[i].PendingRelations.Count == 0)
                {
                    continue;
                }

                var sourceUuid = metadataItems[i].Uuid;
                foreach (var relation in dtoList[i].PendingRelations)
                {
                    var normalized = NormalizePath(relation.OwnerMediaPath);
                    if (string.IsNullOrWhiteSpace(normalized))
                    {
                        continue;
                    }

                    pending.Add((sourceUuid, normalized, relation.RelationType, relation.Text));
                }
            }

            return pending;
        }

        internal static string? GetPrimaryMediaPath(MetadataItem item)
        {
            try
            {
                var media = item.MediaItems?.FirstOrDefault();
                var partPath = media?.Parts?.FirstOrDefault()?.File;
                return NormalizePath(partPath);
            }
            catch
            {
                return null;
            }
        }

        internal static string? NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                var normalized = Path.GetFullPath(path);
                return normalized.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );
            }
            catch
            {
                return path;
            }
        }
    }

    private static partial class MetadataItemRepositoryLoggerMessages
    {
        [LoggerMessage(
            EventId = 100,
            Level = LogLevel.Warning,
            Message = "Unable to resolve metadata relation owner for path '{OwnerPath}'."
        )]
        public static partial void LogRelationResolutionFailure(ILogger logger, string ownerPath);
    }
}
