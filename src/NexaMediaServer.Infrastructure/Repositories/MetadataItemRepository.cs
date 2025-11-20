// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        return this.context.MetadataItems.AsNoTracking();
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

        // Convert DTOs to entities
        var metadataItems = dtoList.Select(MetadataItemMapper.MapToEntity).ToList();

        var primaryPathByUuid = metadataItems.ToDictionary(
            m => m.Uuid,
            MetadataItemRepositoryHelpers.GetPrimaryMediaPath
        );

        var pendingRelations = MetadataItemRepositoryHelpers.CollectPendingRelations(
            dtoList,
            metadataItems
        );

        // SQLite does not support IncludeGraph. Insert in three phases: MetadataItems -> MediaItems -> MediaParts.
        // Wrap in a transaction for atomicity across phases.
        await using var transaction = await this.context.Database.BeginTransactionAsync(
            cancellationToken
        );

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
            .Join(
                this.context.MetadataItems.AsNoTracking()
                    .Include(item => item.Parent)
                    .ThenInclude(parent => parent!.Parent)
                    .Include(item => item.Children)
                    .ThenInclude(child => child.Children)
                    .Include(item => item.MediaItems)
                    .ThenInclude(media => media.Parts)
                    .Include(item => item.LibrarySection),
                relation => relation.MetadataItemId,
                metadata => metadata.Id,
                (relation, metadata) => new { relation.RelatedMetadataItemId, Extra = metadata }
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return extras
            .GroupBy(x => x.RelatedMetadataItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MetadataItem>)group.Select(x => x.Extra).ToList()
            );
    }

    private async Task<List<MetadataRelation>> ResolveRelationTargetsAsync(
        List<(Guid SourceUuid, string OwnerPath, RelationType RelationType)> pendingRelations,
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

        var unresolved = new List<(int SourceId, string OwnerPath, RelationType RelationType)>();

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
                    }
                );
            }
            else
            {
                unresolved.Add((sourceId, pending.OwnerPath, pending.RelationType));
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

    private static class MetadataItemRepositoryHelpers
    {
        internal static List<(
            Guid SourceUuid,
            string OwnerPath,
            RelationType RelationType
        )> CollectPendingRelations(List<MetadataBaseItem> dtoList, List<MetadataItem> metadataItems)
        {
            var pending = new List<(Guid, string, RelationType)>();

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

                    pending.Add((sourceUuid, normalized, relation.RelationType));
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
