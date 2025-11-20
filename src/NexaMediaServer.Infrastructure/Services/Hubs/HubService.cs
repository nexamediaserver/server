// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Hubs;

/// <summary>
/// Service for retrieving hub definitions and content.
/// </summary>
public sealed partial class HubService : IHubService
{
    private const int DefaultHubItemCount = 20;
    private const string DirectorRole = "Director";

    private readonly IDbContextFactory<MediaServerContext> contextFactory;
    private readonly IHubDefinitionProvider hubDefinitionProvider;

    // Logger field is used by source-generated LoggerMessage methods
#pragma warning disable S1450, S4487
    private readonly ILogger<HubService> logger;
#pragma warning restore S1450, S4487

    /// <summary>
    /// Initializes a new instance of the <see cref="HubService"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="hubDefinitionProvider">The hub definition provider.</param>
    /// <param name="logger">The logger.</param>
    public HubService(
        IDbContextFactory<MediaServerContext> contextFactory,
        IHubDefinitionProvider hubDefinitionProvider,
        ILogger<HubService> logger
    )
    {
        this.contextFactory = contextFactory;
        this.hubDefinitionProvider = hubDefinitionProvider;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HubDefinition>> GetHomeHubDefinitionsAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        // Get all library types the user has access to
        var libraryTypes = await context
            .LibrarySections.Select(ls => ls.Type)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        this.LogRetrievingHomeHubs(userId, libraryTypes.Count);

        // Aggregate hub definitions from all library types
        var allHubs = new List<HubDefinition>();

        foreach (var libraryType in libraryTypes)
        {
            var hubs = this.hubDefinitionProvider.GetDefaultHubs(libraryType, HubContext.Home);
            allHubs.AddRange(hubs);
        }

        // Deduplicate by HubType (keep first occurrence) and re-sort
        var definitions = allHubs
            .GroupBy(h => h.HubType)
            .Select(g => g.First())
            .OrderBy(h => h.SortOrder)
            .ToList();

        // Dynamically populate TopByGenre hubs with a random genre
        return await PopulateRandomGenresAsync(context, definitions, null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HubDefinition>> GetLibraryDiscoverHubDefinitionsAsync(
        Guid librarySectionId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var libraryType = await context
            .LibrarySections.Where(ls => ls.Uuid == librarySectionId)
            .Select(ls => ls.Type)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        this.LogRetrievingLibraryHubs(librarySectionId, libraryType);

        var definitions = this.hubDefinitionProvider.GetDefaultHubs(
            libraryType,
            HubContext.LibraryDiscover
        );

        // Dynamically populate TopByGenre hubs with a random genre
        return await PopulateRandomGenresAsync(
            context,
            definitions,
            librarySectionId,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HubDefinition>> GetItemDetailHubDefinitionsAsync(
        Guid metadataItemId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        var metadataType = await context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId)
            .Select(mi => mi.MetadataType)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        this.LogRetrievingItemDetailHubs(metadataItemId, metadataType);

        return this.hubDefinitionProvider.GetItemDetailHubs(metadataType);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HubItem>> GetHubItemsAsync(
        HubType hubType,
        HubContext context,
        string userId,
        Guid? librarySectionId = null,
        Guid? metadataItemId = null,
        string? filterValue = null,
        int count = DefaultHubItemCount,
        CancellationToken cancellationToken = default
    )
    {
        await using var dbContext = await this.contextFactory.CreateDbContextAsync(
            cancellationToken
        );

        this.LogRetrievingHubItems(hubType, context, count);

        // Special handling for Promoted hub: uses two separate queries with backfill
        if (hubType == HubType.Promoted)
        {
            return await GetPromotedHubItemsAsync(
                dbContext,
                userId,
                librarySectionId,
                count,
                cancellationToken
            );
        }

        IQueryable<MetadataItem> query = hubType switch
        {
            HubType.ContinueWatching => BuildContinueWatchingQuery(
                dbContext,
                userId,
                librarySectionId
            ),
            HubType.RecentlyAdded => BuildRecentlyAddedQuery(dbContext, librarySectionId),
            HubType.RecentlyReleased => BuildRecentlyReleasedQuery(dbContext, librarySectionId),
            HubType.RecentlyPlayed => BuildRecentlyPlayedQuery(dbContext, userId, librarySectionId),
            HubType.OnDeck => BuildOnDeckQuery(dbContext, userId, librarySectionId),
            HubType.RecentlyAired => BuildRecentlyAiredQuery(dbContext, librarySectionId),
            HubType.MoreFromDirector => BuildMoreFromDirectorQuery(dbContext, metadataItemId),
            HubType.MoreFromArtist => BuildMoreFromArtistQuery(dbContext, metadataItemId),
            HubType.RelatedCollection => BuildRelatedCollectionQuery(dbContext, metadataItemId),
            HubType.SimilarItems => BuildSimilarItemsQuery(dbContext, metadataItemId),
            HubType.Extras => BuildExtrasQuery(dbContext, metadataItemId),
            HubType.TopByGenre => BuildTopByGenreQuery(dbContext, librarySectionId, filterValue),
            HubType.TopByDirector => BuildTopByDirectorQuery(
                dbContext,
                librarySectionId,
                filterValue
            ),
            HubType.TopByArtist => BuildTopByArtistQuery(dbContext, librarySectionId, filterValue),
            _ => dbContext.MetadataItems.Take(0), // Empty query for unsupported hub types
        };

        var items = await query
            .Take(count)
            .Select(mi => new
            {
                mi.Uuid,
                mi.Title,
                mi.Year,
                mi.ThumbUri,
                mi.MetadataType,
                mi.Duration,
                LibrarySectionUuid = mi.LibrarySection.Uuid,
                ViewOffset = mi
                    .Settings.Where(s => s.UserId == userId)
                    .Select(s => s.ViewOffset)
                    .FirstOrDefault(),
                mi.Tagline,
                mi.ArtUri,
                mi.ArtHash,
                mi.LogoUri,
                mi.LogoHash,
                mi.ContentRating,
                mi.Summary,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items
            .Select(i => new HubItem(
                i.Uuid,
                i.Title,
                i.Year,
                i.ThumbUri,
                i.MetadataType,
                i.Duration,
                i.ViewOffset,
                i.LibrarySectionUuid,
                i.Tagline,
                i.ArtUri,
                i.ArtHash,
                i.LogoUri,
                i.LogoHash,
                i.ContentRating,
                i.Summary
            ))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HubPerson>> GetHubPeopleAsync(
        HubType hubType,
        Guid metadataItemId,
        string userId,
        int count = DefaultHubItemCount,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        this.LogRetrievingHubPeople(hubType, metadataItemId, count);

        // Cast and Crew are distinguished by relation type:
        // - PersonPerformsInVideo (100) = cast/performers
        // - PersonContributesCrewToVideo (101) = crew (director, writer, etc. stored in Text field)
        // - PersonContributesMusicToVideo (102) = music contributors
        var relationTypes = hubType switch
        {
            HubType.Cast => new[] { RelationType.PersonPerformsInVideo },
            HubType.Crew => new[]
            {
                RelationType.PersonContributesCrewToVideo,
                RelationType.PersonContributesMusicToVideo,
            },
            _ => Array.Empty<RelationType>(),
        };

        if (relationTypes.Length == 0)
        {
            return [];
        }

        // Relations store: MetadataItemId -> RelatedMetadataItemId (person)
        // where MetadataItem is the movie/show and RelatedMetadataItem is the person
        return await context
            .MetadataRelations.Where(r => r.RelatedMetadataItem!.Uuid == metadataItemId)
            .Where(r => relationTypes.Contains(r.RelationType))
            .OrderBy(r => r.Id) // Order by creation order as proxy for billing order
            .Take(count)
            .Select(r => new HubPerson(
                r.MetadataItem.Uuid,
                r.MetadataItem.Title,
                r.Text, // Character name for cast, role type for crew
                r.MetadataItem.ThumbUri,
                r.RelationType.ToString()
            ))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<HubConfiguration> UpdateHomeHubConfigurationAsync(
        string userId,
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        this.LogUpdatingHomeHubConfiguration(userId);

        var existingConfig = await context
            .UserHubConfigurations.FirstOrDefaultAsync(
                c =>
                    c.UserId == userId
                    && c.Context == HubContext.Home
                    && c.LibrarySectionId == null,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserHubConfiguration
            {
                UserId = userId,
                Context = HubContext.Home,
            };
            context.UserHubConfigurations.Add(existingConfig);
        }

        existingConfig.EnabledHubTypes = configuration.EnabledHubTypes.ToList();
        existingConfig.DisabledHubTypes = configuration.DisabledHubTypes.ToList();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task<HubConfiguration> UpdateLibraryHubConfigurationAsync(
        Guid librarySectionId,
        string userId,
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        this.LogUpdatingLibraryHubConfiguration(librarySectionId, userId);

        var librarySection = await context
            .LibrarySections.FirstOrDefaultAsync(
                ls => ls.Uuid == librarySectionId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (librarySection == null)
        {
            return configuration;
        }

        var existingConfig = await context
            .UserHubConfigurations.FirstOrDefaultAsync(
                c =>
                    c.UserId == userId
                    && c.Context == HubContext.LibraryDiscover
                    && c.LibrarySectionId == librarySection.Id,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserHubConfiguration
            {
                UserId = userId,
                Context = HubContext.LibraryDiscover,
                LibrarySectionId = librarySection.Id,
            };
            context.UserHubConfigurations.Add(existingConfig);
        }

        existingConfig.EnabledHubTypes = configuration.EnabledHubTypes.ToList();
        existingConfig.DisabledHubTypes = configuration.DisabledHubTypes.ToList();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return configuration;
    }

    private static IQueryable<MetadataItem> BuildContinueWatchingQuery(
        MediaServerContext context,
        string userId,
        Guid? librarySectionId
    )
    {
        var query = context
            .MetadataItems.Where(mi =>
                mi.Settings.Any(s => s.UserId == userId && s.ViewOffset > 0 && s.ViewCount == 0)
            )
            .Where(mi =>
                mi.MetadataType == MetadataType.Movie || mi.MetadataType == MetadataType.Episode
            );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderByDescending(mi =>
            mi.Settings.Where(s => s.UserId == userId).Max(s => s.LastViewedAt)
        );
    }

    private static IQueryable<MetadataItem> BuildRecentlyAddedQuery(
        MediaServerContext context,
        Guid? librarySectionId
    )
    {
        var query = context
            .MetadataItems.Where(mi => mi.ParentId == null) // Root items only
            .Where(mi =>
                mi.MetadataType == MetadataType.Movie
                || mi.MetadataType == MetadataType.Show
                || mi.MetadataType == MetadataType.AlbumRelease
            );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderByDescending(mi => mi.CreatedAt);
    }

    private static IQueryable<MetadataItem> BuildRecentlyReleasedQuery(
        MediaServerContext context,
        Guid? librarySectionId
    )
    {
        var query = context
            .MetadataItems.Where(mi => mi.ParentId == null)
            .Where(mi => mi.ReleaseDate != null)
            .Where(mi =>
                mi.MetadataType == MetadataType.Movie
                || mi.MetadataType == MetadataType.Show
                || mi.MetadataType == MetadataType.AlbumRelease
            );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderByDescending(mi => mi.ReleaseDate);
    }

    private static IQueryable<MetadataItem> BuildRecentlyPlayedQuery(
        MediaServerContext context,
        string userId,
        Guid? librarySectionId
    )
    {
        var query = context.MetadataItems.Where(mi =>
            mi.Settings.Any(s => s.UserId == userId && s.LastViewedAt != null)
        );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderByDescending(mi =>
            mi.Settings.Where(s => s.UserId == userId).Max(s => s.LastViewedAt)
        );
    }

    private static IQueryable<MetadataItem> BuildOnDeckQuery(
        MediaServerContext context,
        string userId,
        Guid? librarySectionId
    )
    {
        // On Deck: Next unwatched episode after the last watched one in a series
        // This is a simplified version - a full implementation would be more complex
        var query = context
            .MetadataItems.Where(mi => mi.MetadataType == MetadataType.Episode)
            .Where(mi => !mi.Settings.Any(s => s.UserId == userId && s.ViewCount > 0))
            .Where(mi =>
                mi.Parent != null
                && mi.Parent.Parent != null
                && mi.Parent.Parent.Children.Any(season =>
                    season.Children.Any(ep =>
                        ep.Settings.Any(s => s.UserId == userId && s.ViewCount > 0)
                    )
                )
            );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderBy(mi => mi.Parent!.Index).ThenBy(mi => mi.Index);
    }

    private static IQueryable<MetadataItem> BuildRecentlyAiredQuery(
        MediaServerContext context,
        Guid? librarySectionId
    )
    {
        var query = context
            .MetadataItems.Where(mi => mi.MetadataType == MetadataType.Episode)
            .Where(mi => mi.ReleaseDate != null)
            .Where(mi => mi.ReleaseDate <= DateOnly.FromDateTime(DateTime.UtcNow));

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        return query.OrderByDescending(mi => mi.ReleaseDate);
    }

    private static IQueryable<MetadataItem> BuildMoreFromDirectorQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Find directors of the current item via PersonContributesCrewToVideo with Text = "Director"
        var directorIds = context
            .MetadataRelations.Where(r => r.RelatedMetadataItem!.Uuid == metadataItemId.Value)
            .Where(r => r.RelationType == RelationType.PersonContributesCrewToVideo)
            .Where(r => r.Text == DirectorRole)
            .Select(r => r.MetadataItemId);

        // Find other items by those directors
        return context
            .MetadataItems.Where(mi => mi.Uuid != metadataItemId.Value)
            .Where(mi =>
                mi.IncomingRelations.Any(r =>
                    r.RelationType == RelationType.PersonContributesCrewToVideo
                    && r.Text == DirectorRole
                    && directorIds.Contains(r.MetadataItemId)
                )
            )
            .OrderByDescending(mi => mi.Year);
    }

    private static IQueryable<MetadataItem> BuildMoreFromArtistQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Find artists of the current item via PersonContributesToAudio
        var artistIds = context
            .MetadataRelations.Where(r => r.RelatedMetadataItem!.Uuid == metadataItemId.Value)
            .Where(r => r.RelationType == RelationType.PersonContributesToAudio)
            .Select(r => r.MetadataItemId);

        // Find other items by those artists
        return context
            .MetadataItems.Where(mi => mi.Uuid != metadataItemId.Value)
            .Where(mi =>
                mi.IncomingRelations.Any(r =>
                    r.RelationType == RelationType.PersonContributesToAudio
                    && artistIds.Contains(r.MetadataItemId)
                )
            )
            .OrderByDescending(mi => mi.Year);
    }

    private static IQueryable<MetadataItem> BuildRelatedCollectionQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Collections are stored via PlaylistCuratesCollection or CollectionAggregatesPlaylist
        // For now, find items that share the same parent collection
        var itemId = context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId.Value)
            .Select(mi => mi.Id)
            .FirstOrDefault();

        var collectionIds = context
            .MetadataRelations.Where(r => r.MetadataItemId == itemId)
            .Where(r =>
                r.RelationType == RelationType.CollectionAggregatesPlaylist
                || r.RelationType == RelationType.PlaylistCuratesCollection
            )
            .Select(r => r.RelatedMetadataItemId);

        // Find other items in those collections
        return context
            .MetadataItems.Where(mi => mi.Uuid != metadataItemId.Value)
            .Where(mi =>
                mi.OutgoingRelations.Any(r =>
                    (
                        r.RelationType == RelationType.CollectionAggregatesPlaylist
                        || r.RelationType == RelationType.PlaylistCuratesCollection
                    ) && collectionIds.Contains(r.RelatedMetadataItemId)
                )
            )
            .OrderBy(mi => mi.Year);
    }

    private static IQueryable<MetadataItem> BuildSimilarItemsQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Similar items would need a dedicated relation type - for now return empty
        // In the future, this could use genre/tag similarity scoring
        return context.MetadataItems.Take(0);
    }

    private static IQueryable<MetadataItem> BuildExtrasQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Find extras related to the item via ClipSupplementsMetadata or TrailerPromotesMetadata
        var itemId = context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId.Value)
            .Select(mi => mi.Id)
            .FirstOrDefault();

        return context
            .MetadataItems.Where(mi =>
                mi.OutgoingRelations.Any(r =>
                    r.RelatedMetadataItemId == itemId
                    && (
                        r.RelationType == RelationType.ClipSupplementsMetadata
                        || r.RelationType == RelationType.TrailerPromotesMetadata
                    )
                )
            )
            .OrderBy(mi => mi.CreatedAt);
    }

    private static IQueryable<MetadataItem> BuildTopByGenreQuery(
        MediaServerContext context,
        Guid? librarySectionId,
        string? genreName
    )
    {
        // Filter to movies only (not people or other metadata types)
        var query = context
            .MetadataItems.Where(mi => mi.ParentId == null)
            .Where(mi => mi.MetadataType == MetadataType.Movie);

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        // Filter by genre if specified
        if (!string.IsNullOrWhiteSpace(genreName))
        {
            query = query.Where(mi => mi.Genres.Any(g => g.Name == genreName));
        }

        // Order by popularity (total view count across all users) descending, then by year
        return query
            .OrderByDescending(mi => mi.Settings.Sum(s => (int?)s.ViewCount) ?? 0)
            .ThenByDescending(mi => mi.Year);
    }

    private static IQueryable<MetadataItem> BuildTopByDirectorQuery(
        MediaServerContext context,
        Guid? librarySectionId,
        string? directorName
    )
    {
        var query = context
            .MetadataItems.Where(mi => mi.ParentId == null)
            .Where(mi => mi.MetadataType == MetadataType.Movie);

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        if (!string.IsNullOrEmpty(directorName))
        {
            query = query.Where(mi =>
                mi.IncomingRelations.Any(r =>
                    r.RelationType == RelationType.PersonContributesCrewToVideo
                    && r.Text == DirectorRole
                    && r.MetadataItem.Title == directorName
                )
            );
        }
        else
        {
            // If no specific director, get movies from any director
            query = query.Where(mi =>
                mi.IncomingRelations.Any(r =>
                    r.RelationType == RelationType.PersonContributesCrewToVideo
                    && r.Text == DirectorRole
                )
            );
        }

        return query.OrderByDescending(mi => mi.CreatedAt);
    }

    private static IQueryable<MetadataItem> BuildTopByArtistQuery(
        MediaServerContext context,
        Guid? librarySectionId,
        string? artistName
    )
    {
        var query = context
            .MetadataItems.Where(mi => mi.ParentId == null)
            .Where(mi =>
                mi.MetadataType == MetadataType.AlbumRelease
                || mi.MetadataType == MetadataType.AlbumReleaseGroup
            );

        if (librarySectionId.HasValue)
        {
            query = query.Where(mi => mi.LibrarySection.Uuid == librarySectionId.Value);
        }

        if (!string.IsNullOrEmpty(artistName))
        {
            query = query.Where(mi =>
                mi.IncomingRelations.Any(r =>
                    r.RelationType == RelationType.PersonContributesToAudio
                    && r.MetadataItem.Title == artistName
                )
            );
        }

        return query.OrderByDescending(mi => mi.CreatedAt);
    }

    /// <summary>
    /// Gets promoted hub items, backfilling with recently added items if needed.
    /// </summary>
    /// <remarks>
    /// Uses two separate queries for clear counting:
    /// 1. Fetch promoted root items (IsPromoted = true, ParentId = null) ordered by PromotedAt DESC.
    /// 2. If count is less than requested, backfill with recently added root items (excluding already fetched).
    /// </remarks>
    private static async Task<IReadOnlyList<HubItem>> GetPromotedHubItemsAsync(
        MediaServerContext dbContext,
        string userId,
        Guid? librarySectionId,
        int count,
        CancellationToken cancellationToken
    )
    {
        const int PromotedHubFixedCount = 10;
        var targetCount = Math.Min(count, PromotedHubFixedCount);

        // Query 1: Get promoted root items
        var promotedQuery = dbContext
            .MetadataItems.Where(mi => mi.IsPromoted)
            .Where(mi => mi.ParentId == null) // Root items only
            .Where(mi =>
                mi.MetadataType == MetadataType.Movie
                || mi.MetadataType == MetadataType.Show
                || mi.MetadataType == MetadataType.AlbumRelease
            );

        if (librarySectionId.HasValue)
        {
            promotedQuery = promotedQuery.Where(mi =>
                mi.LibrarySection.Uuid == librarySectionId.Value
            );
        }

        var promotedItems = await promotedQuery
            .OrderByDescending(mi => mi.PromotedAt)
            .Take(targetCount)
            .Select(mi => new
            {
                mi.Uuid,
                mi.Title,
                mi.Year,
                mi.ThumbUri,
                mi.MetadataType,
                mi.Duration,
                LibrarySectionUuid = mi.LibrarySection.Uuid,
                ViewOffset = mi
                    .Settings.Where(s => s.UserId == userId)
                    .Select(s => s.ViewOffset)
                    .FirstOrDefault(),
                mi.Tagline,
                mi.ArtUri,
                mi.ArtHash,
                mi.LogoUri,
                mi.LogoHash,
                mi.ContentRating,
                mi.Summary,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var promotedCount = promotedItems.Count;
        var backfillCount = targetCount - promotedCount;

        // If we have enough promoted items, return them
        if (backfillCount <= 0)
        {
            return promotedItems
                .Select(i => new HubItem(
                    i.Uuid,
                    i.Title,
                    i.Year,
                    i.ThumbUri,
                    i.MetadataType,
                    i.Duration,
                    i.ViewOffset,
                    i.LibrarySectionUuid,
                    i.Tagline,
                    i.ArtUri,
                    i.ArtHash,
                    i.LogoUri,
                    i.LogoHash,
                    i.ContentRating,
                    i.Summary
                ))
                .ToList();
        }

        // Query 2: Backfill with recently added root items (excluding promoted items)
        var backfillQuery = dbContext
            .MetadataItems.Where(mi => !mi.IsPromoted) // Exclude promoted items
            .Where(mi => mi.ParentId == null) // Root items only
            .Where(mi =>
                mi.MetadataType == MetadataType.Movie
                || mi.MetadataType == MetadataType.Show
                || mi.MetadataType == MetadataType.AlbumRelease
            );

        if (librarySectionId.HasValue)
        {
            backfillQuery = backfillQuery.Where(mi =>
                mi.LibrarySection.Uuid == librarySectionId.Value
            );
        }

        var backfillItems = await backfillQuery
            .OrderByDescending(mi => mi.CreatedAt)
            .Take(backfillCount)
            .Select(mi => new
            {
                mi.Uuid,
                mi.Title,
                mi.Year,
                mi.ThumbUri,
                mi.MetadataType,
                mi.Duration,
                LibrarySectionUuid = mi.LibrarySection.Uuid,
                ViewOffset = mi
                    .Settings.Where(s => s.UserId == userId)
                    .Select(s => s.ViewOffset)
                    .FirstOrDefault(),
                mi.Tagline,
                mi.ArtUri,
                mi.ArtHash,
                mi.LogoUri,
                mi.LogoHash,
                mi.ContentRating,
                mi.Summary,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Combine promoted items first, then backfill items
        var allItems = promotedItems.Concat(backfillItems);

        return allItems
            .Select(i => new HubItem(
                i.Uuid,
                i.Title,
                i.Year,
                i.ThumbUri,
                i.MetadataType,
                i.Duration,
                i.ViewOffset,
                i.LibrarySectionUuid,
                i.Tagline,
                i.ArtUri,
                i.ArtHash,
                i.LogoUri,
                i.LogoHash,
                i.ContentRating,
                i.Summary
            ))
            .ToList();
    }

    /// <summary>
    /// Populates TopByGenre hub definitions with a random genre if no filter value is set.
    /// Updates both the FilterValue and Title to reflect the selected genre.
    /// </summary>
    private static async Task<IReadOnlyList<HubDefinition>> PopulateRandomGenresAsync(
        MediaServerContext context,
        IReadOnlyList<HubDefinition> definitions,
        Guid? librarySectionId,
        CancellationToken cancellationToken
    )
    {
        var result = new List<HubDefinition>();

        foreach (var definition in definitions)
        {
            // Only process TopByGenre hubs without a filter value
            if (
                definition.HubType == HubType.TopByGenre
                && string.IsNullOrWhiteSpace(definition.FilterValue)
            )
            {
                // Get all genres that have movies, then select randomly in memory
                var genres = await context
                    .Genres.Where(g =>
                        g.MetadataItems.Any(mi =>
                            mi.ParentId == null
                            && mi.MetadataType == MetadataType.Movie
                            && (
                                !librarySectionId.HasValue
                                || mi.LibrarySection.Uuid == librarySectionId.Value
                            )
                        )
                    )
                    .Select(g => g.Name)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Select a random genre from the list
                var randomGenre =
                    genres.Count > 0 ? genres[Random.Shared.Next(genres.Count)] : null;

                if (randomGenre != null)
                {
                    // Create new definition with updated filter value and title
                    result.Add(
                        definition with
                        {
                            FilterValue = randomGenre,
                            Title = $"Top {randomGenre} Movies",
                        }
                    );
                }
                else
                {
                    // No genres found, keep original definition
                    result.Add(definition);
                }
            }
            else
            {
                // Keep other hub definitions as-is
                result.Add(definition);
            }
        }

        return result;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving home hub definitions for user {UserId} with {LibraryCount} library types"
    )]
    private partial void LogRetrievingHomeHubs(string userId, int libraryCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving library discover hubs for library {LibrarySectionId} of type {LibraryType}"
    )]
    private partial void LogRetrievingLibraryHubs(Guid librarySectionId, LibraryType libraryType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving item detail hubs for item {MetadataItemId} of type {MetadataType}"
    )]
    private partial void LogRetrievingItemDetailHubs(
        Guid metadataItemId,
        MetadataType metadataType
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving hub items for {HubType} in {Context} context (count: {Count})"
    )]
    private partial void LogRetrievingHubItems(HubType hubType, HubContext context, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieving hub people for {HubType} on item {MetadataItemId} (count: {Count})"
    )]
    private partial void LogRetrievingHubPeople(HubType hubType, Guid metadataItemId, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating home hub configuration for user {UserId}"
    )]
    private partial void LogUpdatingHomeHubConfiguration(string userId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating library hub configuration for library {LibrarySectionId} for user {UserId}"
    )]
    private partial void LogUpdatingLibraryHubConfiguration(Guid librarySectionId, string userId);
}
