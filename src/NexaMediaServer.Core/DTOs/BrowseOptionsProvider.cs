// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Provides static methods to get available browse options based on library type.
/// </summary>
public static class BrowseOptionsProvider
{
    /// <summary>
    /// Gets the available root item types for browsing a library of the specified type.
    /// </summary>
    /// <param name="libraryType">The type of library.</param>
    /// <returns>A list of browsable item type options.</returns>
    public static IReadOnlyList<BrowsableItemTypeOption> GetAvailableRootItemTypes(LibraryType libraryType)
    {
        return libraryType switch
        {
            // Movies: Only movies, dropdown should be hidden
            LibraryType.Movies => [new("Movies", [MetadataType.Movie])],

            // TV Shows: Shows, Seasons, Episodes
            LibraryType.TVShows =>
            [
                new("Shows", [MetadataType.Show]),
                new("Seasons", [MetadataType.Season]),
                new("Episodes", [MetadataType.Episode]),
            ],

            // Music: Albums or Artists (Person/Group with albums)
            LibraryType.Music =>
            [
                new("Albums", [MetadataType.AlbumReleaseGroup]),
                new("Artists", [MetadataType.Person, MetadataType.Group]),
            ],

            // Music Videos: Only movies
            LibraryType.MusicVideos => [new("Music Videos", [MetadataType.Movie])],

            // Home Videos: Only movies
            LibraryType.HomeVideos => [new("Home Videos", [MetadataType.Movie])],

            // Audiobooks: Edition groups
            LibraryType.Audiobooks => [new("Audiobooks", [MetadataType.EditionGroup])],

            // Podcasts: Shows and Episodes
            LibraryType.Podcasts =>
            [
                new("Podcasts", [MetadataType.Show]),
                new("Episodes", [MetadataType.Episode]),
            ],

            // Photos: Photo albums
            LibraryType.Photos => [new("Albums", [MetadataType.PhotoAlbum])],

            // Pictures: Picture sets and pictures
            LibraryType.Pictures =>
            [
                new("Sets", [MetadataType.PictureSet]),
                new("Pictures", [MetadataType.Picture]),
            ],

            // Books: Edition groups
            LibraryType.Books => [new("Books", [MetadataType.EditionGroup])],

            // Comics: Book series and edition groups
            LibraryType.Comics =>
            [
                new("Series", [MetadataType.BookSeries]),
                new("Issues", [MetadataType.EditionGroup]),
            ],

            // Manga: Book series and edition groups
            LibraryType.Manga =>
            [
                new("Series", [MetadataType.BookSeries]),
                new("Volumes", [MetadataType.EditionGroup]),
            ],

            // Magazines: Book series
            LibraryType.Magazines => [new("Magazines", [MetadataType.BookSeries])],

            // Games: Games
            LibraryType.Games => [new("Games", [MetadataType.Game])],

            _ => [new("Items", [MetadataType.Unknown])],
        };
    }

    /// <summary>
    /// Gets the available sort fields for browsing a library of the specified type.
    /// </summary>
    /// <param name="libraryType">The type of library.</param>
    /// <returns>A list of sort field options.</returns>
    public static IReadOnlyList<SortFieldOption> GetAvailableSortFields(LibraryType libraryType)
    {
        return libraryType switch
        {
            // Movies: Title, Year, Release Date, Content Rating, Duration, Date Added
            LibraryType.Movies =>
            [
                new("title", "Title", RequiresUserDataJoin: false),
                new("year", "Year", RequiresUserDataJoin: false),
                new("releaseDate", "Release Date", RequiresUserDataJoin: false),
                new("contentRating", "Content Rating", RequiresUserDataJoin: false),
                new("duration", "Duration", RequiresUserDataJoin: false),
                new("dateAdded", "Date Added", RequiresUserDataJoin: false),
            ],

            // TV Shows: Title, Year, Release Date, Content Rating, Date Added
            LibraryType.TVShows =>
            [
                new("title", "Title", RequiresUserDataJoin: false),
                new("year", "Year", RequiresUserDataJoin: false),
                new("releaseDate", "Release Date", RequiresUserDataJoin: false),
                new("contentRating", "Content Rating", RequiresUserDataJoin: false),
                new("dateAdded", "Date Added", RequiresUserDataJoin: false),
            ],

            // Music: Title, Year, Release Date, Date Added
            LibraryType.Music =>
            [
                new("title", "Title", RequiresUserDataJoin: false),
                new("year", "Year", RequiresUserDataJoin: false),
                new("releaseDate", "Release Date", RequiresUserDataJoin: false),
                new("dateAdded", "Date Added", RequiresUserDataJoin: false),
            ],

            // Default for other library types
            _ =>
            [
                new("title", "Title", RequiresUserDataJoin: false),
                new("year", "Year", RequiresUserDataJoin: false),
                new("releaseDate", "Release Date", RequiresUserDataJoin: false),
                new("dateAdded", "Date Added", RequiresUserDataJoin: false),
            ],
        };
    }
}
