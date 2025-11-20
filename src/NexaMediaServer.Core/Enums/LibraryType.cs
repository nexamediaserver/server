// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of media library.
/// </summary>
public enum LibraryType
{
    // ----------------------------- Video Libraries -----------------------------

    /// <summary>
    /// Movies library containing feature films, short films, and documentaries.
    /// </summary>
    Movies = 1,

    /// <summary>
    /// TV Shows library containing series, seasons, and episodes.
    /// </summary>
    TVShows = 2,

    /// <summary>
    /// Music Videos library containing standalone music video content.
    /// </summary>
    MusicVideos = 3,

    /// <summary>
    /// Home Videos library for personal video recordings.
    /// </summary>
    HomeVideos = 4,

    // ----------------------------- Audio Libraries -----------------------------

    /// <summary>
    /// Music library containing albums, tracks, and recordings.
    /// </summary>
    Music = 10,

    /// <summary>
    /// Audiobooks library containing narrated book content.
    /// </summary>
    Audiobooks = 11,

    /// <summary>
    /// Podcasts library containing podcast series and episodes.
    /// </summary>
    Podcasts = 12,

    // ----------------------------- Photo Libraries -----------------------------

    /// <summary>
    /// Photos library containing personal photographs and albums.
    /// </summary>
    Photos = 20,

    /// <summary>
    /// Pictures library containing digital art, wallpapers, and images.
    /// </summary>
    Pictures = 21,

    // ----------------------------- Book Libraries -----------------------------

    /// <summary>
    /// Books library containing novels, non-fiction, and written literature.
    /// </summary>
    Books = 30,

    /// <summary>
    /// Comics library containing comic books and graphic novels.
    /// </summary>
    Comics = 31,

    /// <summary>
    /// Manga library containing Japanese manga series.
    /// </summary>
    Manga = 32,

    /// <summary>
    /// Magazines library containing periodical publications.
    /// </summary>
    Magazines = 33,

    // ----------------------------- Game Libraries -----------------------------

    /// <summary>
    /// Games library containing video games across all platforms.
    /// </summary>
    Games = 40,
}
