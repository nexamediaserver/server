// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Well-known hint keys used during embedded metadata extraction and scanning.
/// </summary>
/// <remarks>
/// <para>
/// These constants define standardized keys for the hints dictionary produced by
/// <c>IEmbeddedMetadataExtractor</c> implementations. Hints are intermediate values
/// that flow through the scan pipeline and get mapped to entity properties or
/// <c>ExtraFields</c>/<c>ExternalIdentifiers</c> during persistence.
/// </para>
/// <para>
/// Hint keys are based on MusicBrainz Picard tag names for consistency with
/// industry-standard audio tagging conventions.
/// </para>
/// </remarks>
public static class EmbeddedMetadataHintKeys
{
    // -------------------------------- Basic Artist/Album --------------------------------

    /// <summary>
    /// Track artist name (first performer for single-value contexts).
    /// </summary>
    public const string Artist = "artist";

    /// <summary>
    /// Album artist name (first album artist for single-value contexts).
    /// </summary>
    public const string AlbumArtist = "album_artist";

    /// <summary>
    /// Album title.
    /// </summary>
    public const string Album = "album";

    /// <summary>
    /// Disc number within the release.
    /// </summary>
    public const string Disc = "disc";

    /// <summary>
    /// Total number of discs in the release.
    /// </summary>
    public const string DiscCount = "disc_count";

    /// <summary>
    /// Total number of tracks on the disc.
    /// </summary>
    public const string TrackCount = "track_count";

    /// <summary>
    /// All performers as an array.
    /// </summary>
    public const string Performers = "performers";

    /// <summary>
    /// All album artists as an array.
    /// </summary>
    public const string AlbumArtists = "album_artists";

    /// <summary>
    /// Genre names as an array.
    /// </summary>
    public const string Genres = "genres";

    // -------------------------------- Sort Names --------------------------------

    /// <summary>
    /// Album sort name.
    /// </summary>
    public const string AlbumSort = "album_sort";

    /// <summary>
    /// Title sort name.
    /// </summary>
    public const string TitleSort = "title_sort";

    /// <summary>
    /// Artist sort name.
    /// </summary>
    public const string ArtistSort = "artist_sort";

    /// <summary>
    /// Album artist sort name.
    /// </summary>
    public const string AlbumArtistSort = "album_artist_sort";

    /// <summary>
    /// Composer sort name.
    /// </summary>
    public const string ComposerSort = "composer_sort";

    // -------------------------------- Release Information --------------------------------

    /// <summary>
    /// Barcode (UPC/EAN) of the release.
    /// </summary>
    public const string Barcode = "barcode";

    /// <summary>
    /// Label catalog number(s).
    /// </summary>
    public const string CatalogNumber = "catalog_number";

    /// <summary>
    /// Record label name(s).
    /// </summary>
    public const string Label = "label";

    /// <summary>
    /// Media format (e.g., "CD", "Vinyl").
    /// </summary>
    public const string Media = "media";

    /// <summary>
    /// Release type (e.g., "album", "single", "ep").
    /// </summary>
    public const string ReleaseType = "release_type";

    /// <summary>
    /// Release status (e.g., "official", "promotional", "bootleg").
    /// </summary>
    public const string ReleaseStatus = "release_status";

    /// <summary>
    /// Release country (ISO 3166-1 alpha-2).
    /// </summary>
    public const string ReleaseCountry = "release_country";

    /// <summary>
    /// Script used for track listing (ISO 15924).
    /// </summary>
    public const string Script = "script";

    /// <summary>
    /// Original release date (YYYY-MM-DD).
    /// </summary>
    public const string OriginalDate = "original_date";

    /// <summary>
    /// Original release year.
    /// </summary>
    public const string OriginalYear = "original_year";

    /// <summary>
    /// Disc subtitle (media title for specific disc).
    /// </summary>
    public const string DiscSubtitle = "disc_subtitle";

    /// <summary>
    /// Compilation flag ("1" for Various Artists).
    /// </summary>
    public const string Compilation = "compilation";

    /// <summary>
    /// Official website URL.
    /// </summary>
    public const string Website = "website";

    // -------------------------------- Recording Information --------------------------------

    /// <summary>
    /// International Standard Recording Code.
    /// </summary>
    public const string Isrc = "isrc";

    /// <summary>
    /// Beats per minute.
    /// </summary>
    public const string Bpm = "bpm";

    /// <summary>
    /// Musical key.
    /// </summary>
    public const string Key = "key";

    /// <summary>
    /// Copyright message.
    /// </summary>
    public const string Copyright = "copyright";

    /// <summary>
    /// Lyrics text.
    /// </summary>
    public const string Lyrics = "lyrics";

    /// <summary>
    /// Comment/disambiguation text.
    /// </summary>
    public const string Comment = "comment";

    /// <summary>
    /// Encoder name.
    /// </summary>
    public const string EncodedBy = "encoded_by";

    /// <summary>
    /// Encoder settings.
    /// </summary>
    public const string EncoderSettings = "encoder_settings";

    // -------------------------------- Credits/Relationships --------------------------------

    /// <summary>
    /// Composer names.
    /// </summary>
    public const string Composers = "composers";

    /// <summary>
    /// Conductor names.
    /// </summary>
    public const string Conductors = "conductors";

    /// <summary>
    /// Lyricist names.
    /// </summary>
    public const string Lyricists = "lyricists";

    /// <summary>
    /// Arranger names.
    /// </summary>
    public const string Arrangers = "arrangers";

    /// <summary>
    /// Producer names.
    /// </summary>
    public const string Producers = "producers";

    /// <summary>
    /// Engineer names.
    /// </summary>
    public const string Engineers = "engineers";

    /// <summary>
    /// Mixer names.
    /// </summary>
    public const string Mixers = "mixers";

    /// <summary>
    /// DJ mixer names.
    /// </summary>
    public const string DjMixers = "dj_mixers";

    /// <summary>
    /// Remixer names.
    /// </summary>
    public const string Remixers = "remixers";

    /// <summary>
    /// Director names (video/audio director).
    /// </summary>
    public const string Directors = "directors";

    /// <summary>
    /// Writer names (general songwriters).
    /// </summary>
    public const string Writers = "writers";

    /// <summary>
    /// Performer credits with roles (dictionary: role → names).
    /// </summary>
    public const string PerformerCredits = "performer_credits";

    // -------------------------------- Classical Music --------------------------------

    /// <summary>
    /// Work title (overall composition name).
    /// </summary>
    public const string Work = "work";

    /// <summary>
    /// Movement title.
    /// </summary>
    public const string Movement = "movement";

    /// <summary>
    /// Movement number (Arabic numerals).
    /// </summary>
    public const string MovementNumber = "movement_number";

    /// <summary>
    /// Total movements in the work.
    /// </summary>
    public const string MovementTotal = "movement_total";

    /// <summary>
    /// Show work and movement flag.
    /// </summary>
    public const string ShowMovement = "show_movement";

    /// <summary>
    /// Work lyric language (ISO 639-3).
    /// </summary>
    public const string Language = "language";

    /// <summary>
    /// License URL or identifier.
    /// </summary>
    public const string License = "license";

    // -------------------------------- External IDs --------------------------------

    /// <summary>
    /// Dictionary of external identifiers (provider → id).
    /// </summary>
    public const string ExternalIds = "external_ids";

    /// <summary>
    /// Dictionary of external identifiers for artists (provider → id or ids).
    /// </summary>
    public const string ArtistExternalIds = "artist_external_ids";

    /// <summary>
    /// Dictionary of external identifiers for album artists (provider → id or ids).
    /// </summary>
    public const string AlbumArtistExternalIds = "album_artist_external_ids";

    // -------------------------------- MusicBrainz Specific --------------------------------

    /// <summary>
    /// MusicBrainz Recording ID.
    /// </summary>
    public const string MusicBrainzRecordingId = "musicbrainz_recording";

    /// <summary>
    /// MusicBrainz Track ID.
    /// </summary>
    public const string MusicBrainzTrackId = "musicbrainz_track";

    /// <summary>
    /// MusicBrainz Release ID.
    /// </summary>
    public const string MusicBrainzReleaseId = "musicbrainz_release";

    /// <summary>
    /// MusicBrainz Release Group ID.
    /// </summary>
    public const string MusicBrainzReleaseGroupId = "musicbrainz_release_group";

    /// <summary>
    /// MusicBrainz Artist ID(s) - may be semicolon-separated.
    /// </summary>
    public const string MusicBrainzArtistId = "musicbrainz_artist";

    /// <summary>
    /// MusicBrainz Release Artist ID(s) - may be semicolon-separated.
    /// </summary>
    public const string MusicBrainzReleaseArtistId = "musicbrainz_release_artist";

    /// <summary>
    /// MusicBrainz Work ID.
    /// </summary>
    public const string MusicBrainzWorkId = "musicbrainz_work";

    /// <summary>
    /// MusicBrainz Disc ID.
    /// </summary>
    public const string MusicBrainzDiscId = "musicbrainz_disc";

    /// <summary>
    /// MusicBrainz Original Album ID (for merged releases).
    /// </summary>
    public const string MusicBrainzOriginalAlbumId = "musicbrainz_original_album";

    /// <summary>
    /// MusicBrainz Original Artist ID (for merged recordings).
    /// </summary>
    public const string MusicBrainzOriginalArtistId = "musicbrainz_original_artist";

    /// <summary>
    /// AcoustID fingerprint identifier.
    /// </summary>
    public const string AcoustId = "acoustid";

    /// <summary>
    /// AcoustID audio fingerprint data.
    /// </summary>
    public const string AcoustIdFingerprint = "acoustid_fingerprint";

    /// <summary>
    /// Amazon ASIN.
    /// </summary>
    public const string AmazonAsin = "amazon";

    /// <summary>
    /// MusicIP PUID (legacy).
    /// </summary>
    public const string MusicIpPuid = "musicip_puid";

    /// <summary>
    /// MusicIP fingerprint (legacy).
    /// </summary>
    public const string MusicIpFingerprint = "musicip_fingerprint";
}
