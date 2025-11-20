// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Well-known external identifier provider names.
/// </summary>
/// <remarks>
/// <para>
/// Provider names are used as keys in the <see cref="Entities.ExternalIdentifier"/> table
/// to identify the source of each external identifier. Use these constants to ensure
/// consistent naming across the application.
/// </para>
/// <para>
/// For providers with multiple identifier types (e.g., MusicBrainz), use namespaced keys
/// to distinguish between them.
/// </para>
/// </remarks>
public static class ExternalIdProviders
{
    // -------------------------------- Video Providers --------------------------------

    /// <summary>
    /// The Movie Database (TMDB) identifier.
    /// </summary>
    public const string Tmdb = "tmdb";

    /// <summary>
    /// TheTVDB identifier.
    /// </summary>
    public const string Tvdb = "tvdb";

    /// <summary>
    /// IMDb identifier (e.g., "tt0137523").
    /// </summary>
    public const string Imdb = "imdb";

    /// <summary>
    /// Trakt.tv identifier.
    /// </summary>
    public const string Trakt = "trakt";

    // -------------------------------- Music Providers --------------------------------

    /// <summary>
    /// MusicBrainz recording identifier.
    /// </summary>
    /// <remarks>
    /// A recording represents a unique audio track (one specific performance).
    /// </remarks>
    public const string MusicBrainzRecording = "musicbrainz_recording";

    /// <summary>
    /// MusicBrainz track identifier.
    /// </summary>
    /// <remarks>
    /// A track is a recording as it appears on a specific release medium.
    /// </remarks>
    public const string MusicBrainzTrack = "musicbrainz_track";

    /// <summary>
    /// MusicBrainz release identifier.
    /// </summary>
    /// <remarks>
    /// A release represents a specific album release (CD, vinyl, digital, etc.).
    /// </remarks>
    public const string MusicBrainzRelease = "musicbrainz_release";

    /// <summary>
    /// MusicBrainz release group identifier.
    /// </summary>
    /// <remarks>
    /// A release group represents an album concept (all releases of the same album).
    /// </remarks>
    public const string MusicBrainzReleaseGroup = "musicbrainz_release_group";

    /// <summary>
    /// MusicBrainz artist identifier.
    /// </summary>
    public const string MusicBrainzArtist = "musicbrainz_artist";

    /// <summary>
    /// MusicBrainz release artist identifier.
    /// </summary>
    /// <remarks>
    /// The artist credited on the release (album artist).
    /// </remarks>
    public const string MusicBrainzReleaseArtist = "musicbrainz_release_artist";

    /// <summary>
    /// MusicBrainz work identifier.
    /// </summary>
    /// <remarks>
    /// A work represents a distinct intellectual/artistic creation (a song, composition).
    /// </remarks>
    public const string MusicBrainzWork = "musicbrainz_work";

    /// <summary>
    /// Discogs release identifier.
    /// </summary>
    public const string DiscogsRelease = "discogs_release";

    /// <summary>
    /// Discogs master release identifier.
    /// </summary>
    public const string DiscogsMaster = "discogs_master";

    /// <summary>
    /// Discogs artist identifier.
    /// </summary>
    public const string DiscogsArtist = "discogs_artist";

    /// <summary>
    /// Amazon Standard Identification Number.
    /// </summary>
    public const string Amazon = "amazon";

    /// <summary>
    /// MusicIP PUID (legacy fingerprinting system).
    /// </summary>
    public const string MusicIpPuid = "musicip_puid";

    /// <summary>
    /// AcoustID audio fingerprint.
    /// </summary>
    public const string AcoustId = "acoustid";

    // -------------------------------- Book Providers --------------------------------

    /// <summary>
    /// ISBN-10 or ISBN-13 identifier.
    /// </summary>
    public const string Isbn = "isbn";

    /// <summary>
    /// Open Library identifier.
    /// </summary>
    public const string OpenLibrary = "openlibrary";

    /// <summary>
    /// Goodreads book identifier.
    /// </summary>
    public const string Goodreads = "goodreads";

    /// <summary>
    /// Google Books identifier.
    /// </summary>
    public const string GoogleBooks = "google_books";

    // -------------------------------- Game Providers --------------------------------

    /// <summary>
    /// IGDB (Internet Games Database) identifier.
    /// </summary>
    public const string Igdb = "igdb";

    /// <summary>
    /// TheGamesDB identifier.
    /// </summary>
    public const string TheGamesDb = "thegamesdb";

    /// <summary>
    /// Steam application identifier.
    /// </summary>
    public const string Steam = "steam";

    /// <summary>
    /// GOG.com game identifier.
    /// </summary>
    public const string Gog = "gog";
}
