// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of hub, defining the content and logic for populating it.
/// </summary>
public enum HubType
{
    // ----------------------------- Playback-based Hubs -----------------------------

    /// <summary>
    /// Items currently being watched (has view offset but not completed).
    /// </summary>
    ContinueWatching = 1,

    /// <summary>
    /// The next episode to watch in a series (On Deck).
    /// </summary>
    OnDeck = 2,

    /// <summary>
    /// Items recently played/listened to.
    /// </summary>
    RecentlyPlayed = 3,

    // ----------------------------- Time-based Hubs -----------------------------

    /// <summary>
    /// Items recently added to the library.
    /// </summary>
    RecentlyAdded = 10,

    /// <summary>
    /// Items recently released (by release date).
    /// </summary>
    RecentlyReleased = 11,

    /// <summary>
    /// Recently aired episodes.
    /// </summary>
    RecentlyAired = 12,

    /// <summary>
    /// Admin-promoted items for the hero carousel, backfilled with recently added items.
    /// </summary>
    Promoted = 13,

    // ----------------------------- Discovery Hubs -----------------------------

    /// <summary>
    /// Top items filtered by a specific genre.
    /// </summary>
    TopByGenre = 20,

    /// <summary>
    /// Top items by a specific director.
    /// </summary>
    TopByDirector = 21,

    /// <summary>
    /// Top items by a specific artist.
    /// </summary>
    TopByArtist = 22,

    // ----------------------------- Relationship-based Hubs (Detail Page) -----------------------------

    /// <summary>
    /// Cast members for an item.
    /// </summary>
    Cast = 30,

    /// <summary>
    /// Crew members for an item.
    /// </summary>
    Crew = 31,

    /// <summary>
    /// More items from the same director.
    /// </summary>
    MoreFromDirector = 32,

    /// <summary>
    /// More items from the same artist.
    /// </summary>
    MoreFromArtist = 33,

    /// <summary>
    /// Items from the same collection.
    /// </summary>
    RelatedCollection = 34,

    /// <summary>
    /// Similar items based on genre/tags.
    /// </summary>
    SimilarItems = 35,

    /// <summary>
    /// Extras associated with an item (trailers, behind-the-scenes, etc.).
    /// </summary>
    Extras = 36,

    /// <summary>
    /// Tracks from an album release, grouped by medium/disc.
    /// </summary>
    Tracks = 37,

    /// <summary>
    /// Album releases within an album release group.
    /// </summary>
    AlbumReleases = 38,

    /// <summary>
    /// Photos or pictures within a PhotoAlbum or PictureSet.
    /// </summary>
    Photos = 39,
}
