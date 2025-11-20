// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing metadata items.
/// </summary>
public class MetadataItemRepository : IMetadataItemRepository
{
    private readonly MediaServerContext context;
    private readonly IMetadataItemUpdateNotifier updateNotifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    /// <param name="updateNotifier">Notifier used to publish update events.</param>
    public MetadataItemRepository(
        MediaServerContext context,
        IMetadataItemUpdateNotifier updateNotifier
    )
    {
        this.context = context;
        this.updateNotifier = updateNotifier;
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
        IEnumerable<MetadataItem> items,
        CancellationToken cancellationToken = default
    )
    {
        // SQLite does not support IncludeGraph. Insert in three phases: MetadataItems -> MediaItems -> MediaParts.
        // Wrap in a transaction for atomicity across phases.
        await using var transaction = await this.context.Database.BeginTransactionAsync(
            cancellationToken
        );

        // 1) Insert MetadataItems, then reload Ids by querying back via Uuid
        var metadataItems = (items ?? Enumerable.Empty<MetadataItem>()).ToList();
        if (metadataItems.Count == 0)
        {
            return;
        }

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
}
