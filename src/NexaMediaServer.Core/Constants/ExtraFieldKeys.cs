// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Well-known keys for the <see cref="Entities.MetadataItem.ExtraFields"/> dictionary.
/// </summary>
/// <remarks>
/// <para>
/// These constants define standardized keys for storing typed metadata values in the
/// JSON-backed <c>ExtraFields</c> dictionary. Using constants ensures consistency
/// across the application and enables compile-time checking of key names.
/// </para>
/// <para>
/// Values stored under these keys should match the documented types. Use
/// <see cref="Helpers.ExtraFieldsAccessor"/> for type-safe access with validation.
/// </para>
/// </remarks>
public static class ExtraFieldKeys
{
    // -------------------------------- Audio Release Fields --------------------------------

    /// <summary>
    /// Release type (e.g., "album", "single", "ep", "compilation", "soundtrack").
    /// </summary>
    /// <remarks>Type: string (multi-value supported as semicolon-separated).</remarks>
    public const string ReleaseType = "release_type";

    /// <summary>
    /// Release status (e.g., "official", "promotional", "bootleg", "pseudo-release").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string ReleaseStatus = "release_status";

    /// <summary>
    /// Release country as ISO 3166-1 alpha-2 code (e.g., "US", "GB", "JP").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string ReleaseCountry = "release_country";

    /// <summary>
    /// Label catalog number for the release.
    /// </summary>
    /// <remarks>Type: string (multi-value supported as semicolon-separated).</remarks>
    public const string CatalogNumber = "catalog_number";

    /// <summary>
    /// Script used for the release's track list (ISO 15924, e.g., "Latn", "Jpan").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Script = "script";

    /// <summary>
    /// Original release date (YYYY-MM-DD format).
    /// </summary>
    /// <remarks>Type: string (ISO 8601 date).</remarks>
    public const string OriginalDate = "original_date";

    /// <summary>
    /// Original release year.
    /// </summary>
    /// <remarks>Type: int.</remarks>
    public const string OriginalYear = "original_year";

    /// <summary>
    /// Whether this is a compilation (Various Artists).
    /// </summary>
    /// <remarks>Type: bool (stored as "1" for true, absent or "0" for false).</remarks>
    public const string Compilation = "compilation";

    // -------------------------------- Audio Medium Fields --------------------------------

    /// <summary>
    /// Media format (e.g., "CD", "Vinyl", "Digital Media", "Cassette").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string MediaFormat = "media_format";

    /// <summary>
    /// Disc subtitle (media title for a specific disc in a multi-disc release).
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string DiscSubtitle = "disc_subtitle";

    // -------------------------------- Audio Work Fields --------------------------------

    /// <summary>
    /// Work title (overall work name, e.g., "Symphony No. 5 in C minor, op. 67").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string WorkTitle = "work_title";

    /// <summary>
    /// Movement title (e.g., "Andante con moto").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string MovementTitle = "movement_title";

    /// <summary>
    /// Movement number in Arabic numerals.
    /// </summary>
    /// <remarks>Type: int.</remarks>
    public const string MovementNumber = "movement_number";

    /// <summary>
    /// Total number of movements in the work.
    /// </summary>
    /// <remarks>Type: int.</remarks>
    public const string MovementTotal = "movement_total";

    /// <summary>
    /// Whether to display work and movement instead of track title.
    /// </summary>
    /// <remarks>Type: bool (stored as "1" for true).</remarks>
    public const string ShowMovement = "show_movement";

    /// <summary>
    /// Work lyric language as ISO 639-3 code.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Language = "language";

    /// <summary>
    /// License URL or identifier for the track/recording.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string License = "license";

    // -------------------------------- Track/Recording Fields --------------------------------

    /// <summary>
    /// Album sort name for ordering purposes.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string AlbumSort = "album_sort";

    /// <summary>
    /// Title sort name for ordering purposes.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string TitleSort = "title_sort";

    /// <summary>
    /// Artist sort name for ordering purposes.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string ArtistSort = "artist_sort";

    /// <summary>
    /// Album artist sort name for ordering purposes.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string AlbumArtistSort = "album_artist_sort";

    /// <summary>
    /// Composer sort name for ordering purposes.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string ComposerSort = "composer_sort";

    /// <summary>
    /// Beats per minute of the track.
    /// </summary>
    /// <remarks>Type: int.</remarks>
    public const string Bpm = "bpm";

    /// <summary>
    /// Musical key of the track (e.g., "C major", "A minor").
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Key = "key";

    /// <summary>
    /// Copyright message for the recording.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Copyright = "copyright";

    /// <summary>
    /// Lyrics text for the track.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Lyrics = "lyrics";

    /// <summary>
    /// Comment or disambiguation text.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Comment = "comment";

    /// <summary>
    /// Encoder software or settings used.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string EncodedBy = "encoded_by";

    /// <summary>
    /// Encoder settings string.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string EncoderSettings = "encoder_settings";

    /// <summary>
    /// Official artist/release website URL.
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string Website = "website";

    // -------------------------------- Label/Company Fields --------------------------------

    /// <summary>
    /// Record label names (multi-value supported as semicolon-separated).
    /// </summary>
    /// <remarks>Type: string.</remarks>
    public const string LabelName = "label_name";
}
