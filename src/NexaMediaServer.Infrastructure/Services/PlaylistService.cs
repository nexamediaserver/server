// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Playback;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service for managing playback playlists including generation, navigation, and chunk retrieval.
/// </summary>
public partial class PlaylistService : IPlaylistService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> MaterializationLocks = new();

    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<PlaylistService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating DbContext instances.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaylistService(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ILogger<PlaylistService> logger
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PlaylistGenerator> CreatePlaylistAsync(
        int playbackSessionId,
        PlaylistSeed seed,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Generate shuffle seed if shuffle is enabled
        string? shuffleState = null;
        if (seed.Shuffle)
        {
            shuffleState = GenerateShuffleSeed();
        }

        var generator = new PlaylistGenerator
        {
            PlaybackSessionId = playbackSessionId,
            SeedJson = JsonSerializer.Serialize(seed),
            Cursor = seed.StartIndex,
            Shuffle = seed.Shuffle,
            Repeat = seed.Repeat,
            ShuffleState = shuffleState,
            ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays),
            ChunkSize = PlaybackDefaults.PlaylistChunkSize,
        };

        await db.PlaylistGenerators.AddAsync(generator, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Materialize initial items
        await this.MaterializeItemsAsync(
            generator,
            seed.StartIndex + PlaybackDefaults.MinimumLookahead,
            cancellationToken
        );

        return generator;
    }

    /// <inheritdoc />
    public async Task<PlaylistChunkResponse> GetPlaylistChunkAsync(
        int sessionId,
        PlaylistChunkRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators
            .AsNoTracking()
            .Include(g => g.PlaybackSession)
                .ThenInclude(s => s.CapabilityProfile)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == request.PlaylistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            LogPlaylistNotFound(this.logger, request.PlaylistGeneratorId);
            return new PlaylistChunkResponse
            {
                PlaylistGeneratorId = request.PlaylistGeneratorId,
                Items = [],
                TotalCount = 0,
            };
        }

        // Ensure items are materialized up to the requested range
        await this.MaterializeItemsAsync(
            generator,
            request.StartIndex + request.Limit,
            cancellationToken
        );

        // Re-fetch items after materialization
        var items = await db
            .PlaylistGeneratorItems
            .AsNoTracking()
            .Include(i => i.MetadataItem)
                .ThenInclude(m => m!.Parent)
                    .ThenInclude(p => p!.Parent)
                        .ThenInclude(album => album!.IncomingRelations.Where(r =>
                            r.RelationType == RelationType.PersonContributesToAudio ||
                            r.RelationType == RelationType.GroupContributesToAudio))
                        .ThenInclude(r => r.MetadataItem)
            .Include(i => i.MetadataItem)
                .ThenInclude(m => m!.IncomingRelations.Where(r =>
                    r.RelationType == RelationType.PersonContributesToAudio ||
                    r.RelationType == RelationType.GroupContributesToAudio))
                .ThenInclude(r => r.MetadataItem)
            .Include(i => i.MediaPart)
            .Include(i => i.MediaItem)
                .ThenInclude(mi => mi!.Parts)
            .Where(i => i.PlaylistGeneratorId == generator.Id)
            .OrderBy(i => i.SortOrder)
            .Skip(request.StartIndex)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var playlistItems = items
            .Select(
                (item, idx) =>
                {
                    string? playbackUrl = ResolvePlaybackUrl(
                        item,
                        generator.PlaybackSession?.CapabilityProfile?.Capabilities
                    );

                    return new PlaylistItem
                    {
                        ItemEntryId = item.Id,
                        MetadataItemId = item.MetadataItemId,
                        MetadataItemUuid = item.MetadataItem?.Uuid ?? Guid.Empty,
                        MediaItemId = item.MediaItemId,
                        MediaPartId = item.MediaPartId,
                        Index = request.StartIndex + idx,
                        Served = item.Served,
                        Title = item.MetadataItem?.Title ?? "Unknown",
                        MetadataType = item.MetadataItem?.MetadataType.ToString() ?? "Unknown",
                        DurationMs =
                            item.MetadataItem?.Duration != null
                                ? item.MetadataItem.Duration.Value * 1000L
                                : null,
                        ThumbUri = item.MetadataItem?.ThumbUri,
                        ParentTitle = ResolveParentTitle(item.MetadataItem),
                        Subtitle = FormatSubtitle(item.MetadataItem),
                        PlaybackUrl = playbackUrl,
                        PrimaryPerson = ResolvePrimaryPerson(item.MetadataItem),
                    };
                }
            )
            .ToList();

        var totalCount = await GetTotalCountAsync(db, generator, cancellationToken);

        return new PlaylistChunkResponse
        {
            PlaylistGeneratorId = generator.PlaylistGeneratorId,
            Items = playlistItems,
            CurrentIndex = generator.Cursor,
            TotalCount = totalCount,
            HasMore = request.StartIndex + items.Count < totalCount,
            Shuffle = generator.Shuffle,
            Repeat = generator.Repeat,
        };
    }

    /// <inheritdoc />
    public async Task<PlaylistItem?> GetNextItemAsync(
        int sessionId,
        Guid playlistGeneratorId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators.Include(g => g.PlaybackSession)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == playlistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            return null;
        }

        int nextIndex = generator.Cursor + 1;
        var totalCount = await GetTotalCountAsync(db, generator, cancellationToken);

        // Handle end of playlist
        if (nextIndex >= totalCount)
        {
            if (generator.Repeat && totalCount > 0)
            {
                nextIndex = 0;
            }
            else
            {
                return null;
            }
        }

        // Ensure item is materialized
        await this.MaterializeItemsAsync(
            generator,
            nextIndex + PlaybackDefaults.MinimumLookahead,
            cancellationToken
        );

        // Advance cursor
        generator.Cursor = nextIndex;
        await db.SaveChangesAsync(cancellationToken);

        return await GetItemAtIndexAsync(db, generator.Id, nextIndex, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlaylistItem?> GetPreviousItemAsync(
        int sessionId,
        Guid playlistGeneratorId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators.Include(g => g.PlaybackSession)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == playlistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            return null;
        }

        int prevIndex = generator.Cursor - 1;

        // Handle beginning of playlist
        if (prevIndex < 0)
        {
            var totalCount = await GetTotalCountAsync(db, generator, cancellationToken);
            if (generator.Repeat && totalCount > 0)
            {
                prevIndex = totalCount - 1;
                // Ensure item is materialized if wrapping
                await this.MaterializeItemsAsync(generator, prevIndex + 1, cancellationToken);
            }
            else
            {
                return null;
            }
        }

        // Move cursor
        generator.Cursor = prevIndex;
        await db.SaveChangesAsync(cancellationToken);

        return await GetItemAtIndexAsync(db, generator.Id, prevIndex, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlaylistItem?> JumpToIndexAsync(
        int sessionId,
        Guid playlistGeneratorId,
        int index,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators.Include(g => g.PlaybackSession)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == playlistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            return null;
        }

        if (index < 0)
        {
            return null;
        }

        var totalCount = await GetTotalCountAsync(db, generator, cancellationToken);
        if (index >= totalCount)
        {
            return null;
        }

        // Ensure item is materialized
        await this.MaterializeItemsAsync(
            generator,
            index + PlaybackDefaults.MinimumLookahead,
            cancellationToken
        );

        // Set cursor
        generator.Cursor = index;
        await db.SaveChangesAsync(cancellationToken);

        return await GetItemAtIndexAsync(db, generator.Id, index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetShuffleAsync(
        int sessionId,
        Guid playlistGeneratorId,
        bool enabled,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators.Include(g => g.PlaybackSession)
            .Include(g => g.Items)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == playlistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            return false;
        }

        generator.Shuffle = enabled;

        if (enabled)
        {
            // Re-shuffle items that haven't been served yet
            await ReshuffleItemsAsync(db, generator, cancellationToken);
        }
        else
        {
            // Restore original order for unserved items
            await RestoreOriginalOrderAsync(db, generator, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetRepeatAsync(
        int sessionId,
        Guid playlistGeneratorId,
        bool enabled,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var generator = await db
            .PlaylistGenerators.Include(g => g.PlaybackSession)
            .FirstOrDefaultAsync(
                g =>
                    g.PlaylistGeneratorId == playlistGeneratorId
                    && g.PlaybackSession.SessionId == sessionId,
                cancellationToken
            );

        if (generator == null)
        {
            return false;
        }

        generator.Repeat = enabled;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> MaterializeItemsAsync(
        PlaylistGenerator generator,
        int upToIndex,
        CancellationToken cancellationToken
    )
    {
        var materializationLock = MaterializationLocks.GetOrAdd(
            generator.Id,
            _ => new SemaphoreSlim(1, 1)
        );

        await materializationLock.WaitAsync(cancellationToken);

        try
        {
            await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existingCount = await db
                .PlaylistGeneratorItems.AsNoTracking().Where(i => i.PlaylistGeneratorId == generator.Id)
                .CountAsync(cancellationToken);

            if (existingCount > upToIndex)
            {
                // All requested items are already materialized
                return 0;
            }

            var seed = JsonSerializer.Deserialize<PlaylistSeed>(generator.SeedJson) ?? new PlaylistSeed();

            // For shuffled playlists, we need to determine total count first to materialize correctly
            // For non-shuffled playlists, we can resolve incrementally
            List<int> itemsToMaterialize;
            if (generator.Shuffle && existingCount > 0)
            {
                // For shuffled playlists with existing items, retrieve from persisted items
                // by fetching the MetadataItemIds at positions we need (already shuffled)
                var totalAvailable = await GetTotalCountAsync(db, generator, cancellationToken);
                var needed = Math.Min(upToIndex - existingCount + 1, totalAvailable - existingCount);

                if (needed <= 0)
                {
                    return 0;
                }

                // We need to resolve more items from the seed - the seeded shuffle is deterministic
                itemsToMaterialize = await ResolveItemsForSeedAsync(
                    db,
                    seed,
                    existingCount,
                    needed,
                    generator.Shuffle,
                    generator.ShuffleState,
                    cancellationToken
                );
            }
            else
            {
                itemsToMaterialize = await ResolveItemsForSeedAsync(
                    db,
                    seed,
                    existingCount,
                    upToIndex - existingCount + 1,
                    generator.Shuffle,
                    generator.ShuffleState,
                    cancellationToken
                );
            }

            var newItems = itemsToMaterialize
                .Select(
                    (id, idx) =>
                    {
                        int sortOrder = existingCount + idx;
                        return new PlaylistGeneratorItem
                        {
                            PlaylistGeneratorId = generator.Id,
                            MetadataItemId = id,
                            SortOrder = sortOrder,
                            OriginalSortOrder = sortOrder,
                        };
                    }
                )
                .ToList();

            if (newItems.Count != 0)
            {
                await db.PlaylistGeneratorItems.AddRangeAsync(newItems, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            return newItems.Count;
        }
        finally
        {
            materializationLock.Release();
        }
    }

    private static string? ResolvePlaybackUrl(
        PlaylistGeneratorItem item,
        PlaybackCapabilities? capabilities
    )
    {
        var metadataType = item.MetadataItem?.MetadataType;
        if (
            metadataType
                is not (MetadataType.Photo
                    or MetadataType.PhotoAlbum
                    or MetadataType.Picture
                    or MetadataType.PictureSet)
        )
        {
            return null;
        }

        // Prefer explicitly selected media part; fall back to first available part.
        var partPath = item.MediaPart?.File
            ?? item.MediaItem?.Parts?.OrderBy(p => p.Id).FirstOrDefault()?.File;

        if (string.IsNullOrWhiteSpace(partPath))
        {
            return null;
        }

        var sourceExt = PlaybackFormatSelector.NormalizeExtension(Path.GetExtension(partPath));
        var supportedFormats = capabilities?.SupportedImageFormats ?? [];
        var targetFormat = PlaybackFormatSelector.ChooseImageFormat(sourceExt, supportedFormats);
        var passthrough = string.Equals(sourceExt, targetFormat, StringComparison.OrdinalIgnoreCase);

        var queryBuilder = new StringBuilder();
        queryBuilder.Append("uri=");
        queryBuilder.Append(Uri.EscapeDataString(partPath));

        if (!passthrough)
        {
            queryBuilder.Append("&format=");
            queryBuilder.Append(Uri.EscapeDataString(targetFormat));
        }

        return $"/api/v1/images/transcode?{queryBuilder}";
    }

    private static async Task<List<int>> ResolveItemsForSeedAsync(
        MediaServerContext db,
        PlaylistSeed seed,
        int skip,
        int take,
        bool shuffle,
        string? shuffleSeed,
        CancellationToken cancellationToken
    )
    {
        IQueryable<MetadataItem> query;

        switch (seed.Type.ToLowerInvariant())
        {
            case "single":
                // Single item playlist
                if (seed.OriginatorId.HasValue)
                {
                    return skip == 0 ? [seed.OriginatorId.Value] : [];
                }

                return [];

            case "explicit":
                // Explicitly listed items
                if (seed.Items == null || seed.Items.Count == 0)
                {
                    return [];
                }

                return seed.Items.Skip(skip).Take(take).ToList();

            case "album":
            case "season":
            case "container":
                // Children of a container (album tracks, season episodes)
                if (!seed.OriginatorId.HasValue)
                {
                    return [];
                }

                query = db
                    .MetadataItems.AsNoTracking()
                    .Where(m => m.ParentId == seed.OriginatorId.Value && m.DeletedAt == null)
                    .OrderBy(m => m.Index ?? int.MaxValue)
                    .ThenBy(m => m.Title);
                break;

            case "show":
                // All episodes of a show, ordered by season then episode
                if (!seed.OriginatorId.HasValue)
                {
                    return [];
                }

                // Get seasons first, then episodes within each season
                var seasonIds = await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.Season
                        && m.DeletedAt == null
                    )
                    .OrderBy(m => m.Index ?? int.MaxValue)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                query = db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        seasonIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Episode
                        && m.DeletedAt == null
                    )
                    .OrderBy(m => m.Parent!.Index ?? int.MaxValue)
                    .ThenBy(m => m.Index ?? int.MaxValue);
                break;

            case "artist":
                // All tracks by an artist (via albums)
                if (!seed.OriginatorId.HasValue)
                {
                    return [];
                }

                var albumIds = await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.AlbumRelease
                        && m.DeletedAt == null
                    )
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                query = db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        albumIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Track
                        && m.DeletedAt == null
                    )
                    .OrderBy(m => m.Parent!.Year ?? int.MaxValue)
                    .ThenBy(m => m.Parent!.Title)
                    .ThenBy(m => m.Index ?? int.MaxValue);
                break;

            case "library":
                // All playable items in a library
                if (!seed.LibrarySectionId.HasValue)
                {
                    return [];
                }

                query = db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.LibrarySectionId == seed.LibrarySectionId.Value
                        && m.DeletedAt == null
                        && (
                            m.MetadataType == MetadataType.Movie
                            || m.MetadataType == MetadataType.Episode
                            || m.MetadataType == MetadataType.Track
                        )
                    )
                    .OrderBy(m => m.SortTitle);
                break;

            default:
                return [];
        }

        if (shuffle)
        {
            // For shuffled playlists, we need to fetch all potential items and shuffle
            // deterministically using the stored seed
            var allIds = await query.Select(m => m.Id).ToListAsync(cancellationToken);
            var rng = CreateSeededRandom(shuffleSeed);
            ShuffleList(allIds, rng);
            return allIds.Skip(skip).Take(take).ToList();
        }

        return await query.Select(m => m.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    private static async Task<int> GetTotalCountAsync(
        MediaServerContext db,
        PlaylistGenerator generator,
        CancellationToken cancellationToken
    )
    {
        var seed = JsonSerializer.Deserialize<PlaylistSeed>(generator.SeedJson) ?? new PlaylistSeed();

        switch (seed.Type.ToLowerInvariant())
        {
            case "single":
                return seed.OriginatorId.HasValue ? 1 : 0;

            case "explicit":
                return seed.Items?.Count ?? 0;

            case "album":
            case "season":
            case "container":
                if (!seed.OriginatorId.HasValue)
                {
                    return 0;
                }

                return await db
                    .MetadataItems.AsNoTracking()
                    .Where(m => m.ParentId == seed.OriginatorId.Value && m.DeletedAt == null)
                    .CountAsync(cancellationToken);

            case "show":
                if (!seed.OriginatorId.HasValue)
                {
                    return 0;
                }

                var showSeasonIds = await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.Season
                        && m.DeletedAt == null
                    )
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                return await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        showSeasonIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Episode
                        && m.DeletedAt == null
                    )
                    .CountAsync(cancellationToken);

            case "artist":
                if (!seed.OriginatorId.HasValue)
                {
                    return 0;
                }

                var artistAlbumIds = await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.ParentId == seed.OriginatorId.Value
                        && m.MetadataType == MetadataType.AlbumRelease
                        && m.DeletedAt == null
                    )
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                return await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        artistAlbumIds.Contains(m.ParentId ?? 0)
                        && m.MetadataType == MetadataType.Track
                        && m.DeletedAt == null
                    )
                    .CountAsync(cancellationToken);

            case "library":
                if (!seed.LibrarySectionId.HasValue)
                {
                    return 0;
                }

                return await db
                    .MetadataItems.AsNoTracking()
                    .Where(m =>
                        m.LibrarySectionId == seed.LibrarySectionId.Value
                        && m.DeletedAt == null
                        && (
                            m.MetadataType == MetadataType.Movie
                            || m.MetadataType == MetadataType.Episode
                            || m.MetadataType == MetadataType.Track
                        )
                    )
                    .CountAsync(cancellationToken);

            default:
                return 0;
        }
    }

    private static async Task<PlaylistItem?> GetItemAtIndexAsync(
        MediaServerContext db,
        int generatorId,
        int index,
        CancellationToken cancellationToken
    )
    {
        var item = await db
            .PlaylistGeneratorItems.AsNoTracking()
            .Include(i => i.MetadataItem)
                .ThenInclude(m => m!.Parent)
                    .ThenInclude(p => p!.Parent)
                        .ThenInclude(album => album!.IncomingRelations.Where(r =>
                            r.RelationType == RelationType.PersonContributesToAudio ||
                            r.RelationType == RelationType.GroupContributesToAudio))
                        .ThenInclude(r => r.MetadataItem)
            .Include(i => i.MetadataItem)
                .ThenInclude(m => m!.IncomingRelations.Where(r =>
                    r.RelationType == RelationType.PersonContributesToAudio ||
                    r.RelationType == RelationType.GroupContributesToAudio))
                .ThenInclude(r => r.MetadataItem)
            .Where(i => i.PlaylistGeneratorId == generatorId && i.SortOrder == index)
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
        {
            return null;
        }

        return new PlaylistItem
        {
            ItemEntryId = item.Id,
            MetadataItemId = item.MetadataItemId,
            MetadataItemUuid = item.MetadataItem?.Uuid ?? Guid.Empty,
            MediaItemId = item.MediaItemId,
            MediaPartId = item.MediaPartId,
            Index = index,
            Served = item.Served,
            Title = item.MetadataItem?.Title ?? "Unknown",
            MetadataType = item.MetadataItem?.MetadataType.ToString() ?? "Unknown",
            DurationMs =
                item.MetadataItem?.Duration != null
                    ? item.MetadataItem.Duration.Value * 1000L
                    : null,
            ThumbUri = item.MetadataItem?.ThumbUri,
            ParentTitle = ResolveParentTitle(item.MetadataItem),
            Subtitle = FormatSubtitle(item.MetadataItem),
            PrimaryPerson = ResolvePrimaryPerson(item.MetadataItem),
        };
    }

    private static async Task ReshuffleItemsAsync(
        MediaServerContext db,
        PlaylistGenerator generator,
        CancellationToken cancellationToken
    )
    {
        // Get all unserved items
        var unservedItems = await db
            .PlaylistGeneratorItems.Where(i =>
                i.PlaylistGeneratorId == generator.Id && !i.Served && i.SortOrder > generator.Cursor
            )
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        if (unservedItems.Count <= 1)
        {
            return;
        }

        // Generate a new shuffle seed and store it
        generator.ShuffleState = GenerateShuffleSeed();

        // Shuffle the items using the new seed
        var rng = CreateSeededRandom(generator.ShuffleState);
        var shuffledItems = unservedItems.ToList();
        ShuffleListItems(shuffledItems, rng);

        // Two-phase update to avoid unique constraint violations:
        // Phase 1: Set all items to temporary negative sort orders
        for (int i = 0; i < shuffledItems.Count; i++)
        {
            shuffledItems[i].SortOrder = -(i + 1);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Phase 2: Set items to their final sort orders
        int currentOrder = generator.Cursor + 1;
        foreach (var item in shuffledItems)
        {
            item.SortOrder = currentOrder++;
        }
    }

    private static async Task RestoreOriginalOrderAsync(
        MediaServerContext db,
        PlaylistGenerator generator,
        CancellationToken cancellationToken
    )
    {
        // Restore original order for unserved items using OriginalSortOrder
        var unservedItems = await db
            .PlaylistGeneratorItems.Where(i =>
                i.PlaylistGeneratorId == generator.Id && !i.Served && i.SortOrder > generator.Cursor
            )
            .OrderBy(i => i.OriginalSortOrder)
            .ToListAsync(cancellationToken);

        // Two-phase update to avoid unique constraint violations:
        // Phase 1: Set all items to temporary negative sort orders
        for (int i = 0; i < unservedItems.Count; i++)
        {
            unservedItems[i].SortOrder = -(i + 1);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Phase 2: Set items to their final sort orders based on original order
        int currentOrder = generator.Cursor + 1;
        foreach (var item in unservedItems)
        {
            item.SortOrder = currentOrder++;
        }

        generator.ShuffleState = null;
    }

    private static void ShuffleList(List<int> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private static void ShuffleListItems<T>(List<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    /// Generates a base64-encoded 32-byte random seed for deterministic shuffle.
    /// </summary>
    private static string GenerateShuffleSeed()
    {
        var seedBytes = new byte[32];
        RandomNumberGenerator.Fill(seedBytes);
        return Convert.ToBase64String(seedBytes);
    }

    /// <summary>
    /// Creates a seeded Random instance from a base64-encoded seed.
    /// Uses the first 4 bytes of the seed to derive an integer seed.
    /// </summary>
    private static Random CreateSeededRandom(string? shuffleSeed)
    {
        if (string.IsNullOrEmpty(shuffleSeed))
        {
            // Fallback to current time-based seed if no seed provided
            return new Random();
        }

        try
        {
            var seedBytes = Convert.FromBase64String(shuffleSeed);
            int intSeed = BitConverter.ToInt32(seedBytes, 0);
            return new Random(intSeed);
        }
        catch (FormatException)
        {
            // Invalid base64, fallback to time-based seed
            return new Random();
        }
    }

    /// <summary>
    /// Resolves the parent title for a metadata item.
    /// For tracks, returns the album (Parent.Parent) instead of the medium (Parent).
    /// For other types, returns the direct parent title.
    /// </summary>
    private static string? ResolveParentTitle(MetadataItem? item)
    {
        if (item?.Parent == null)
        {
            return null;
        }

        // For tracks, the Parent is AlbumMedium (Disc 1, etc.)
        // We want the album name, which is Parent.Parent (AlbumRelease)
        if (item.MetadataType == MetadataType.Track && item.Parent.Parent != null)
        {
            return item.Parent.Parent.Title;
        }

        return item.Parent.Title;
    }

    /// <summary>
    /// Resolves the primary person for a metadata item.
    /// For tracks, returns the first person or group contributor, falling back to the album's primary person.
    /// </summary>
    private static MetadataItem? ResolvePrimaryPerson(MetadataItem? item)
    {
        if (item == null || item.MetadataType != MetadataType.Track)
        {
            return null;
        }

        // For tracks, get the first person/group contributor from incoming relations
        // The track is the RelatedMetadataItem, and the person/group is the MetadataItem
        var relation = item.IncomingRelations
            ?.FirstOrDefault(r =>
                r.RelationType == RelationType.PersonContributesToAudio ||
                r.RelationType == RelationType.GroupContributesToAudio);

        if (relation != null)
        {
            return relation.MetadataItem;
        }

        // Fall back to the album's primary person
        // For tracks: Parent = AlbumMedium, Parent.Parent = AlbumRelease
        var album = item.Parent?.Parent;
        if (album == null)
        {
            return null;
        }

        var albumRelation = album.IncomingRelations
            ?.FirstOrDefault(r =>
                r.RelationType == RelationType.PersonContributesToAudio ||
                r.RelationType == RelationType.GroupContributesToAudio);

        return albumRelation?.MetadataItem;
    }

    private static string? FormatSubtitle(MetadataItem? item)
    {
        if (item == null)
        {
            return null;
        }

        return item.MetadataType switch
        {
            MetadataType.Episode => item.Index.HasValue ? $"Episode {item.Index}" : null,
            MetadataType.Track => item.Index.HasValue ? $"Track {item.Index}" : null,
            _ => null,
        };
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Playlist generator {PlaylistGeneratorId} not found"
    )]
    private static partial void LogPlaylistNotFound(ILogger logger, Guid playlistGeneratorId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown playlist seed type: {SeedType}")]
    private static partial void LogUnknownSeedType(ILogger logger, string seedType);
}
