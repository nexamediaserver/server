// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Hubs;

/// <summary>
/// Provides hardcoded hub definitions for different library and metadata types.
/// This implementation is designed to be easily replaced with database-backed definitions in the future.
/// </summary>
public sealed class HubDefinitionProvider : IHubDefinitionProvider
{
    // Movie library hubs for Home and LibraryDiscover contexts
    private static readonly HubDefinition[] MovieHubs =
    [
        new(
            HubType.RecentlyAdded,
            "Recently Added",
            MetadataType.Movie,
            HubContext.LibraryDiscover,
            2
        ),
        new(
            HubType.RecentlyReleased,
            "Recently Released",
            MetadataType.Movie,
            HubContext.LibraryDiscover,
            3
        ),
        new(
            HubType.TopByGenre,
            "Top Movies by Genre",
            MetadataType.Movie,
            HubContext.LibraryDiscover,
            4
        ),
        new(
            HubType.TopByDirector,
            "From Popular Directors",
            MetadataType.Movie,
            HubContext.LibraryDiscover,
            5
        ),
    ];

    // TV Show library hubs
    private static readonly HubDefinition[] TvShowHubs =
    [
        new(HubType.OnDeck, "On Deck", MetadataType.Episode, HubContext.LibraryDiscover, 1),
        new(
            HubType.RecentlyAdded,
            "Recently Added Shows",
            MetadataType.Show,
            HubContext.LibraryDiscover,
            3
        ),
        new(
            HubType.RecentlyAired,
            "Recently Aired",
            MetadataType.Episode,
            HubContext.LibraryDiscover,
            4
        ),
        new(
            HubType.TopByGenre,
            "Top Drama",
            MetadataType.Show,
            HubContext.LibraryDiscover,
            5,
            "Drama"
        ),
    ];

    // Music library hubs
    private static readonly HubDefinition[] MusicHubs =
    [
        new(
            HubType.RecentlyPlayed,
            "Recently Played",
            MetadataType.AlbumRelease,
            HubContext.LibraryDiscover,
            1
        ),
        new(
            HubType.RecentlyAdded,
            "Recently Added",
            MetadataType.AlbumRelease,
            HubContext.LibraryDiscover,
            2
        ),
        new(
            HubType.TopByArtist,
            "Top Artists",
            MetadataType.AlbumRelease,
            HubContext.LibraryDiscover,
            3
        ),
        new(
            HubType.TopByGenre,
            "Top Rock Albums",
            MetadataType.AlbumRelease,
            HubContext.LibraryDiscover,
            4,
            "Rock"
        ),
    ];

    // Movie detail page hubs
    private static readonly HubDefinition[] MovieDetailHubs =
    [
        new(HubType.Cast, "Cast", MetadataType.Person, HubContext.ItemDetail, 1),
        new(HubType.Crew, "Crew", MetadataType.Person, HubContext.ItemDetail, 2),
        new(HubType.Extras, "Extras", MetadataType.Clip, HubContext.ItemDetail, 3),
        new(
            HubType.MoreFromDirector,
            "More from Director",
            MetadataType.Movie,
            HubContext.ItemDetail,
            4
        ),
        new(
            HubType.RelatedCollection,
            "From Collection",
            MetadataType.Movie,
            HubContext.ItemDetail,
            5
        ),
        new(HubType.SimilarItems, "Similar Movies", MetadataType.Movie, HubContext.ItemDetail, 6),
    ];

    // TV Show detail page hubs
    private static readonly HubDefinition[] ShowDetailHubs =
    [
        new(HubType.Cast, "Cast", MetadataType.Person, HubContext.ItemDetail, 1),
        new(HubType.Crew, "Crew", MetadataType.Person, HubContext.ItemDetail, 2),
        new(HubType.SimilarItems, "Similar Shows", MetadataType.Show, HubContext.ItemDetail, 3),
    ];

    // Episode detail page hubs
    private static readonly HubDefinition[] EpisodeDetailHubs =
    [
        new(HubType.Cast, "Guest Stars", MetadataType.Person, HubContext.ItemDetail, 1),
    ];

    // Album detail page hubs
    private static readonly HubDefinition[] AlbumDetailHubs =
    [
        new(HubType.Cast, "Artists", MetadataType.Person, HubContext.ItemDetail, 1),
        new(
            HubType.MoreFromArtist,
            "More from Artist",
            MetadataType.AlbumRelease,
            HubContext.ItemDetail,
            2
        ),
        new(
            HubType.SimilarItems,
            "Similar Albums",
            MetadataType.AlbumRelease,
            HubContext.ItemDetail,
            3
        ),
    ];

    // Default/fallback hubs
    private static readonly HubDefinition[] DefaultHubs =
    [
        new(
            HubType.RecentlyAdded,
            "Recently Added",
            MetadataType.Unknown,
            HubContext.LibraryDiscover,
            1
        ),
    ];

    private static readonly HubDefinition[] EmptyHubs = [];

    // Common hubs shared across multiple library types
    // Continue Watching is applicable to any media type that supports resuming from a position
    private static readonly HubDefinition[] ContinueWatchingHubs =
    [
        new(
            HubType.ContinueWatching,
            "Continue Watching",
            MetadataType.Unknown, // Will be filtered by backend based on actual resumable items
            HubContext.LibraryDiscover,
            1 // Will be positioned after Promoted
        ),
    ];

    // Promoted hub for hero carousel - shows admin-promoted items with recently added backfill
    private static readonly HubDefinition[] PromotedHubs =
    [
        new(
            HubType.Promoted,
            "Featured",
            MetadataType.Unknown, // Global promotion across all metadata types
            HubContext.LibraryDiscover,
            0, // Positioned first
            null,
            HubWidgetType.Hero // Netflix-style hero carousel widget
        ),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<HubDefinition> GetDefaultHubs(LibraryType libraryType, HubContext context)
    {
        if (context == HubContext.ItemDetail)
        {
            return EmptyHubs;
        }

        var hubs = libraryType switch
        {
            LibraryType.Movies => MovieHubs,
            LibraryType.TVShows => TvShowHubs,
            LibraryType.Music => MusicHubs,
            LibraryType.MusicVideos => MovieHubs, // Similar to movies
            LibraryType.HomeVideos => DefaultHubs,
            LibraryType.Audiobooks => MusicHubs, // Similar to music
            LibraryType.Podcasts => MusicHubs,
            _ => DefaultHubs,
        };

        // Prepend Continue Watching hub for library types that support resumable playback
        // (Movies, TV Shows, Audiobooks, Podcasts)
        var supportsContinueWatching =
            libraryType
            is LibraryType.Movies
                or LibraryType.TVShows
                or LibraryType.MusicVideos
                or LibraryType.Audiobooks
                or LibraryType.Podcasts;

        // Prepend common hubs: Promoted (hero carousel) and Continue Watching (if applicable)
        // Order: Promoted -> Continue Watching -> Library-specific hubs
        var commonHubs = supportsContinueWatching
            ? PromotedHubs.Concat(ContinueWatchingHubs)
            : PromotedHubs.AsEnumerable();

        var finalHubs = commonHubs.Concat(hubs).ToArray();

        // For Home context, adjust the context in the returned definitions
        if (context == HubContext.Home)
        {
            return finalHubs.Select(h => h with { Context = HubContext.Home }).ToArray();
        }

        return finalHubs;
    }

    /// <inheritdoc/>
    public IReadOnlyList<HubDefinition> GetItemDetailHubs(MetadataType metadataType)
    {
        return metadataType switch
        {
            MetadataType.Movie => MovieDetailHubs,
            MetadataType.Show => ShowDetailHubs,
            MetadataType.Season => EmptyHubs, // Seasons don't typically have detail hubs
            MetadataType.Episode => EpisodeDetailHubs,
            MetadataType.AlbumRelease or MetadataType.AlbumReleaseGroup => AlbumDetailHubs,
            MetadataType.Track or MetadataType.Recording => EmptyHubs,
            _ => EmptyHubs,
        };
    }
}
