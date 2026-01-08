// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Provides well-known field names for <see cref="Entities.MetadataItem"/> properties
/// used in field locking functionality.
/// </summary>
/// <remarks>
/// <para>
/// These constants provide compile-time safety when referencing built-in metadata fields
/// for locking purposes. Use these constants when checking or setting locked fields to
/// avoid typos and ensure consistency.
/// </para>
/// <para>
/// User-defined dynamic fields are also supported and can use arbitrary string names
/// not defined in this class.
/// </para>
/// </remarks>
public static class MetadataFieldNames
{
    // ----------------------------- Core Metadata Fields -----------------------------

    /// <summary>
    /// The display title of the metadata item.
    /// </summary>
    public const string Title = nameof(Entities.MetadataItem.Title);

    /// <summary>
    /// The sort title used for alphabetical ordering.
    /// </summary>
    public const string SortTitle = nameof(Entities.MetadataItem.SortTitle);

    /// <summary>
    /// The original title in the original language.
    /// </summary>
    public const string OriginalTitle = nameof(Entities.MetadataItem.OriginalTitle);

    /// <summary>
    /// The summary or description text.
    /// </summary>
    public const string Summary = nameof(Entities.MetadataItem.Summary);

    /// <summary>
    /// The tagline or slogan.
    /// </summary>
    public const string Tagline = nameof(Entities.MetadataItem.Tagline);

    /// <summary>
    /// The content rating (e.g., "PG-13", "TV-MA").
    /// </summary>
    public const string ContentRating = nameof(Entities.MetadataItem.ContentRating);

    /// <summary>
    /// The age rating associated with the content rating.
    /// </summary>
    public const string ContentRatingAge = nameof(Entities.MetadataItem.ContentRatingAge);

    /// <summary>
    /// The release date. When locked, this also locks the year field since year is derived from release date.
    /// </summary>
    public const string ReleaseDate = nameof(Entities.MetadataItem.ReleaseDate);

    /// <summary>
    /// The index number within parent collection (e.g., season number, episode number).
    /// </summary>
    public const string Index = nameof(Entities.MetadataItem.Index);

    /// <summary>
    /// The absolute index number across all parent collections.
    /// </summary>
    public const string AbsoluteIndex = nameof(Entities.MetadataItem.AbsoluteIndex);

    /// <summary>
    /// The duration in seconds.
    /// </summary>
    public const string Duration = nameof(Entities.MetadataItem.Duration);

    // ----------------------------- Artwork Fields -----------------------------

    /// <summary>
    /// The thumbnail/poster image.
    /// </summary>
    public const string Thumb = "Thumb";

    /// <summary>
    /// The artwork/backdrop/fanart image.
    /// </summary>
    public const string Art = "Art";

    /// <summary>
    /// The logo/clearlogo image.
    /// </summary>
    public const string Logo = "Logo";

    // ----------------------------- Collection Fields -----------------------------

    /// <summary>
    /// The genres collection.
    /// </summary>
    public const string Genres = nameof(Entities.MetadataItem.Genres);

    /// <summary>
    /// The tags collection.
    /// </summary>
    public const string Tags = nameof(Entities.MetadataItem.Tags);

    /// <summary>
    /// The cast/actors credits.
    /// </summary>
    public const string Cast = "Cast";

    /// <summary>
    /// The crew credits (directors, writers, producers, etc.).
    /// </summary>
    public const string Crew = "Crew";

    /// <summary>
    /// All credits (both cast and crew). Use this to lock all credit-related updates.
    /// </summary>
    public const string Credits = "Credits";

    /// <summary>
    /// The external identifiers collection (TMDB, TVDB, IMDb, etc.).
    /// </summary>
    public const string ExternalIdentifiers = nameof(Entities.MetadataItem.ExternalIdentifiers);
}
