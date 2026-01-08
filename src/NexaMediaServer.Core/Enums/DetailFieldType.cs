// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of field displayed on item detail pages.
/// </summary>
public enum DetailFieldType
{
    // ----------------------------- Actions -----------------------------

    /// <summary>
    /// The actions button block (Play, Edit, Menu, etc.).
    /// This is a non-configurable placeholder; the client determines which buttons to show.
    /// </summary>
    Actions = 0,

    // ----------------------------- Core Metadata Fields -----------------------------

    /// <summary>
    /// The display title of the item.
    /// </summary>
    Title = 1,

    /// <summary>
    /// The original title in the original language.
    /// </summary>
    OriginalTitle = 2,

    /// <summary>
    /// The tagline or slogan.
    /// </summary>
    Tagline = 3,

    /// <summary>
    /// The summary or description text.
    /// </summary>
    Summary = 4,

    // ----------------------------- Date/Time Fields -----------------------------

    /// <summary>
    /// The release date of the item.
    /// </summary>
    ReleaseDate = 10,

    /// <summary>
    /// The release year of the item.
    /// </summary>
    Year = 11,

    /// <summary>
    /// The runtime/duration of the item.
    /// </summary>
    Runtime = 12,

    // ----------------------------- Rating Fields -----------------------------

    /// <summary>
    /// The content rating (e.g., "PG-13", "TV-MA").
    /// </summary>
    ContentRating = 20,

    // ----------------------------- Collection Fields -----------------------------

    /// <summary>
    /// The genres associated with the item.
    /// </summary>
    Genres = 30,

    /// <summary>
    /// The tags associated with the item.
    /// </summary>
    Tags = 31,

    // ----------------------------- External Identifiers -----------------------------

    /// <summary>
    /// External identifiers (TMDB, TVDB, IMDb, etc.).
    /// </summary>
    ExternalIds = 40,

    // ----------------------------- Custom Fields -----------------------------

    /// <summary>
    /// A custom field defined by an administrator, stored in ExtraFields.
    /// </summary>
    Custom = 100,
}
