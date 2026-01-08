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

        var configuration = ToHubConfiguration(
            await GetHubConfigurationEntityAsync(
                context,
                HubContext.Home,
                librarySectionId: null,
                metadataType: null,
                cancellationToken
            ).ConfigureAwait(false)
        );

        definitions = ApplyHubConfiguration(definitions, configuration).ToList();

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

        var libraryInfo = await context
            .LibrarySections.Where(ls => ls.Uuid == librarySectionId)
            .Select(ls => new { ls.Id, ls.Type })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (libraryInfo == null)
        {
            return [];
        }

        this.LogRetrievingLibraryHubs(librarySectionId, libraryInfo.Type);

        var definitions = this.hubDefinitionProvider.GetDefaultHubs(
            libraryInfo.Type,
            HubContext.LibraryDiscover
        );

        var configuration = ToHubConfiguration(
            await GetHubConfigurationEntityAsync(
                context,
                HubContext.LibraryDiscover,
                libraryInfo.Id,
                metadataType: null,
                cancellationToken
            ).ConfigureAwait(false)
        );

        definitions = ApplyHubConfiguration(definitions, configuration).ToList();

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

        var itemInfo = await context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId)
            .Select(mi =>
                new
                {
                    mi.MetadataType,
                    ChildCount = mi.Children.Count,
                    mi.LibrarySectionId,
                }
            )
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (itemInfo == null)
        {
            return [];
        }

        this.LogRetrievingItemDetailHubs(metadataItemId, itemInfo.MetadataType);

        // Pass child count for conditional hub logic (e.g., album release groups)
        var definitions = this.hubDefinitionProvider.GetItemDetailHubs(
            itemInfo.MetadataType,
            itemInfo.ChildCount
        );

        var configuration = ToHubConfiguration(
            await GetHubConfigurationEntityAsync(
                context,
                HubContext.ItemDetail,
                itemInfo.LibrarySectionId,
                itemInfo.MetadataType,
                cancellationToken
            ).ConfigureAwait(false)
        )
            ?? ToHubConfiguration(
                await GetHubConfigurationEntityAsync(
                    context,
                    HubContext.ItemDetail,
                    librarySectionId: null,
                    itemInfo.MetadataType,
                    cancellationToken
                ).ConfigureAwait(false)
            );

        definitions = ApplyHubConfiguration(definitions, configuration).ToList();

        return definitions;
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
            HubType.Tracks => BuildTracksQuery(dbContext, metadataItemId),
            HubType.AlbumReleases => BuildAlbumReleasesQuery(dbContext, metadataItemId),
            HubType.Photos => BuildPhotosQuery(dbContext, metadataItemId),
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
                mi.Index,
                ParentUuid = mi.Parent != null ? mi.Parent.Uuid : (Guid?)null,
                ParentTitle = ResolveParentTitleForHub(mi),
                ParentIndex = mi.Parent != null ? mi.Parent.Index : (int?)null,
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
                i.Summary,
                i.Index,
                i.ParentUuid,
                i.ParentTitle,
                i.ParentIndex
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
    public async Task<HubConfiguration?> GetHubConfigurationAsync(
        HubContext context,
        Guid? librarySectionId,
        MetadataType? metadataType,
        CancellationToken cancellationToken = default
    )
    {
        await using var dbContext = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        int? libraryId = null;

        if (librarySectionId.HasValue)
        {
            libraryId = await dbContext
                .LibrarySections.Where(section => section.Uuid == librarySectionId.Value)
                .Select(section => (int?)section.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!libraryId.HasValue)
            {
                return null;
            }
        }

        return ToHubConfiguration(
            await GetHubConfigurationEntityAsync(
                dbContext,
                context,
                libraryId,
                metadataType,
                cancellationToken
            ).ConfigureAwait(false)
        );
    }

    /// <inheritdoc/>
    public async Task<HubConfiguration> UpdateHomeHubConfigurationAsync(
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        this.LogUpdatingHomeHubConfiguration();

        var existingConfig = await context
            .UserHubConfigurations.FirstOrDefaultAsync(
                c =>
                    c.UserId == null
                    && c.Context == HubContext.Home
                    && c.LibrarySectionId == null
                    && c.MetadataType == null,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserHubConfiguration
            {
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
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        this.LogUpdatingLibraryHubConfiguration(librarySectionId);

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
                    c.UserId == null
                    && c.Context == HubContext.LibraryDiscover
                    && c.LibrarySectionId == librarySection.Id
                    && c.MetadataType == null,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserHubConfiguration
            {
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

    /// <inheritdoc/>
    public async Task<HubConfiguration> UpdateItemDetailHubConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        int? libraryId = null;

        this.LogUpdatingItemDetailHubConfiguration(metadataType, librarySectionId);

        if (librarySectionId.HasValue)
        {
            libraryId = await context
                .LibrarySections.Where(section => section.Uuid == librarySectionId.Value)
                .Select(section => (int?)section.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!libraryId.HasValue)
            {
                return configuration;
            }
        }

        var existingConfig = await context
            .UserHubConfigurations.FirstOrDefaultAsync(
                c =>
                    c.UserId == null
                    && c.Context == HubContext.ItemDetail
                    && c.MetadataType == metadataType
                    && c.LibrarySectionId == libraryId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existingConfig == null)
        {
            existingConfig = new UserHubConfiguration
            {
                Context = HubContext.ItemDetail,
                MetadataType = metadataType,
                LibrarySectionId = libraryId,
            };
            context.UserHubConfigurations.Add(existingConfig);
        }

        existingConfig.EnabledHubTypes = configuration.EnabledHubTypes.ToList();
        existingConfig.DisabledHubTypes = configuration.DisabledHubTypes.ToList();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return configuration;
    }

    private static HubConfiguration? ToHubConfiguration(UserHubConfiguration? entity) =>
        entity == null
            ? null
            : new HubConfiguration(entity.EnabledHubTypes, entity.DisabledHubTypes);

    private static IReadOnlyList<HubDefinition> ApplyHubConfiguration(
        IReadOnlyList<HubDefinition> defaults,
        HubConfiguration? configuration
    )
    {
        if (configuration == null)
        {
            return defaults;
        }

        var disabled = configuration.DisabledHubTypes.ToHashSet();

        var definitionsByType = defaults
            .Where(definition => !disabled.Contains(definition.HubType))
            .GroupBy(definition => definition.HubType)
            .ToDictionary(group => group.Key, group => group.First());

        var ordered = new List<HubDefinition>();

        if (configuration.EnabledHubTypes.Count > 0)
        {
            foreach (var hubType in configuration.EnabledHubTypes)
            {
                if (definitionsByType.TryGetValue(hubType, out var definition))
                {
                    ordered.Add(definition);
                    definitionsByType.Remove(hubType);
                }
            }
        }

        ordered.AddRange(definitionsByType.Values.OrderBy(definition => definition.SortOrder));

        return ordered;
    }

    private static Task<UserHubConfiguration?> GetHubConfigurationEntityAsync(
        MediaServerContext context,
        HubContext hubContext,
        int? librarySectionId,
        MetadataType? metadataType,
        CancellationToken cancellationToken
    )
    {
        return context
            .UserHubConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(
                configuration =>
                    configuration.UserId == null
                    && configuration.Context == hubContext
                    && configuration.LibrarySectionId == librarySectionId
                    && configuration.MetadataType == metadataType,
                cancellationToken
            );
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
        // Build from relations to ensure we only consider artists that actually contribute to audio items.
        // Group by artist to compute contribution counts and total view counts in SQL.
        var audioRelationTypes = new[]
        {
            RelationType.PersonContributesToAudio,
            RelationType.GroupContributesToAudio,
        };

        // Some pipelines may store the artist on MetadataItemId (expected), others might store it
        // on RelatedMetadataItemId; cover both directions to be robust.
        var relations = context.MetadataRelations.Where(r => audioRelationTypes.Contains(r.RelationType));

        var artistAudioPairs = relations.SelectMany(r => new[]
        {
            new
            {
                ArtistId = r.MetadataItem.MetadataType == MetadataType.Person
                    || r.MetadataItem.MetadataType == MetadataType.Group
                        ? (int?)r.MetadataItemId
                        : null,
                AudioItem = r.RelatedMetadataItem,
            },
            new
            {
                ArtistId = r.RelatedMetadataItem.MetadataType == MetadataType.Person
                    || r.RelatedMetadataItem.MetadataType == MetadataType.Group
                        ? (int?)r.RelatedMetadataItemId
                        : null,
                AudioItem = r.MetadataItem,
            },
        }).Where(x => x.ArtistId.HasValue && x.AudioItem != null);

        if (librarySectionId.HasValue)
        {
            artistAudioPairs = artistAudioPairs.Where(x => x.AudioItem!.LibrarySection.Uuid == librarySectionId.Value);
        }

        var grouped = artistAudioPairs
            .GroupBy(x => x.ArtistId!.Value)
            .Select(g => new
            {
                ArtistId = g.Key,
                ContributionCount = g.Count(),
                TotalViews = g.Sum(x => x.AudioItem!.Settings.Sum(s => s.ViewCount)),
            });

        var artists = context.MetadataItems.Where(mi =>
            mi.MetadataType == MetadataType.Person || mi.MetadataType == MetadataType.Group
        );

        if (!string.IsNullOrEmpty(artistName))
        {
            artists = artists.Where(mi => mi.Title == artistName);
        }

        // Join grouped metrics back to artists and order.
        var query = artists
            .Join(
                grouped,
                artist => artist.Id,
                metrics => metrics.ArtistId,
                (artist, metrics) => new { artist, metrics }
            )
            .OrderByDescending(x => x.metrics.ContributionCount)
            .ThenByDescending(x => x.metrics.TotalViews)
            .Select(x => x.artist);

        return query;
    }

    private static IQueryable<MetadataItem> BuildTracksQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Get the item to determine if it's an AlbumReleaseGroup or AlbumRelease
        var item = context
            .MetadataItems.Where(mi => mi.Uuid == metadataItemId.Value)
            .Select(mi => new { mi.Id, mi.MetadataType })
            .FirstOrDefault();

        if (item == null)
        {
            return context.MetadataItems.Take(0);
        }

        // For AlbumReleaseGroup with single release, get tracks from the single child AlbumRelease
        // For AlbumRelease, get tracks directly
        // Tracks are children of AlbumMedium, which is a child of AlbumRelease
        // Structure: AlbumReleaseGroup -> AlbumRelease -> AlbumMedium -> Track
        if (item.MetadataType == MetadataType.AlbumReleaseGroup)
        {
            // Get the single child AlbumRelease's ID
            var albumReleaseId = context
                .MetadataItems.Where(mi => mi.ParentId == item.Id)
                .Where(mi => mi.MetadataType == MetadataType.AlbumRelease)
                .Select(mi => mi.Id)
                .FirstOrDefault();

            if (albumReleaseId == 0)
            {
                return context.MetadataItems.Take(0);
            }

            // Get tracks through AlbumMedium
            return context
                .MetadataItems.Where(mi => mi.Parent != null && mi.Parent.ParentId == albumReleaseId)
                .Where(mi => mi.MetadataType == MetadataType.Track)
                .OrderBy(mi => mi.Parent!.Index) // Medium/disc index
                .ThenBy(mi => mi.Index); // Track index
        }

        // For AlbumRelease, get tracks through its child AlbumMediums
        return context
            .MetadataItems.Where(mi => mi.Parent != null && mi.Parent.ParentId == item.Id)
            .Where(mi => mi.MetadataType == MetadataType.Track)
            .OrderBy(mi => mi.Parent!.Index) // Medium/disc index
            .ThenBy(mi => mi.Index); // Track index
    }

    private static IQueryable<MetadataItem> BuildAlbumReleasesQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Get releases that are children of the AlbumReleaseGroup
        return context
            .MetadataItems.Where(mi =>
                mi.Parent != null && mi.Parent.Uuid == metadataItemId.Value
            )
            .Where(mi => mi.MetadataType == MetadataType.AlbumRelease)
            .OrderBy(mi => mi.Year)
            .ThenBy(mi => mi.Title);
    }

    private static IQueryable<MetadataItem> BuildPhotosQuery(
        MediaServerContext context,
        Guid? metadataItemId
    )
    {
        if (!metadataItemId.HasValue)
        {
            return context.MetadataItems.Take(0);
        }

        // Get photos or pictures that are children of a PhotoAlbum or PictureSet
        return context
            .MetadataItems.Where(mi =>
                mi.Parent != null && mi.Parent.Uuid == metadataItemId.Value
            )
            .Where(mi => mi.MetadataType == MetadataType.Photo || mi.MetadataType == MetadataType.Picture)
            .OrderBy(mi => mi.Index)
            .ThenBy(mi => mi.Title);
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

    /// <summary>
    /// Resolves the parent title for a metadata item in hub queries.
    /// For tracks, returns the album (Parent.Parent) instead of the medium (Parent).
    /// For other types, returns the direct parent title.
    /// </summary>
    private static string? ResolveParentTitleForHub(MetadataItem mi)
    {
        if (mi.Parent == null)
        {
            return null;
        }

        // For tracks, the Parent is AlbumMedium (Disc 1, etc.)
        // We want the album name, which is Parent.Parent (AlbumRelease)
        if (mi.MetadataType == MetadataType.Track && mi.Parent.Parent != null)
        {
            return mi.Parent.Parent.Title;
        }

        return mi.Parent.Title;
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
        Message = "Updating global home hub configuration"
    )]
    private partial void LogUpdatingHomeHubConfiguration();

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating library hub configuration for library {LibrarySectionId}"
    )]
    private partial void LogUpdatingLibraryHubConfiguration(Guid librarySectionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating item detail hub configuration for {MetadataType} and library {LibrarySectionId}"
    )]
    private partial void LogUpdatingItemDetailHubConfiguration(
        MetadataType metadataType,
        Guid? librarySectionId
    );
}
