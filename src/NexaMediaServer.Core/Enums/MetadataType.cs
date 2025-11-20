// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Enumeration of supported metadata types.
/// </summary>
public enum MetadataType
{
    /// <summary>
    /// Unknown or unspecified metadata type.
    /// </summary>
    /// <remarks>
    /// Generally this is only used when an item has been scanned but no metadata
    /// has been resolved yet.
    /// </remarks>
    Unknown = 0,

    // ------------------------------- Video Metadata Types ---------------------------------

    /// <summary>
    /// The metadata represents a movie, either feature-length or short film.
    /// </summary>
    Movie = 1,

    /// <summary>
    /// The metadata represents a TV show.
    /// </summary>
    Show = 2,

    /// <summary>
    /// The metadata represents a single season of a TV show.
    /// </summary>
    Season = 3,

    /// <summary>
    /// The metadata represents an episode of a TV show.
    /// </summary>
    Episode = 4,

    // -------------------------------- Audio Metadata Types --------------------------------

    /// <summary>
    /// The metadata represents a grouping of album releases, such as a studio album or compilation.
    /// </summary>
    AlbumReleaseGroup = 20,

    /// <summary>
    /// The metadata represents a audio album release.
    /// </summary>
    AlbumRelease = 21,

    /// <summary>
    /// The metadata represents a medium within an audio album release, such as a disc in a multi-disc set.
    /// </summary>
    AlbumMedium = 22,

    /// <summary>
    /// The metadata represents a audio track.
    /// </summary>
    Track = 23,

    /// <summary>
    /// The metadata represents a audio recording.
    /// </summary>
    Recording = 24,

    /// <summary>
    /// The metadata represents a audio work.
    /// </summary>
    AudioWork = 25,

    // ------------------------------- Photo Metadata Types ---------------------------------

    /// <summary>
    /// The metadata represents a photo album.
    /// </summary>
    PhotoAlbum = 30,

    /// <summary>
    /// The metadata represents a photo.
    /// </summary>
    /// <remarks>
    /// A photo is a representation of real-world imagery, typically captured using a camera or similar device.
    /// </remarks>
    Photo = 31,

    /// <summary>
    /// The metadata represents a picture set.
    /// </summary>
    PictureSet = 32,

    /// <summary>
    /// The metadata represents a picture.
    /// </summary>
    /// <remarks>
    /// A picture is an image file that may include photographs, illustrations, or other visual representations.
    /// It differs from a photo in that it may not necessarily represent real-world imagery and does not fit
    /// into the date or location-based album structure typically associated with photos.
    /// </remarks>
    Picture = 33,

    // -------------------------------- Book Metadata Types ---------------------------------

    /// <summary>
    /// The metadata represents an ordered set of books, such as a manga series, a periodical,
    /// or a comic book series.
    /// </summary>
    BookSeries = 40,

    /// <summary>
    /// The metadata represents a grouping of book editions, such as a single book released
    /// in multiple formats (hardcover, paperback, eBook, audiobook).
    /// </summary>
    EditionGroup = 41,

    /// <summary>
    /// The metadata represents a concrete publication of a book, such as a specific edition
    /// or format.
    /// </summary>
    Edition = 42,

    /// <summary>
    /// The metadata represents an item within a book edition, such as a chapter or volume.
    /// </summary>
    /// <remarks>
    /// Analogous to a track in a music album.
    /// </remarks>
    EditionItem = 43,

    /// <summary>
    /// The metadata represents a literary work as a whole.
    /// </summary>
    LiteraryWork = 44,

    /// <summary>
    /// The metadata represents a part of a literary work, such as a chapter or section.
    /// </summary>
    /// <remarks>
    /// We purposefully add another level here to be able to cross-reference chapters/sections
    /// across different editions of the same literary work.
    /// For example, being able to link a pre-published chapter of a manga released in a magazine
    /// to the corresponding chapter in the tankōbon edition.
    /// </remarks>
    LiteraryWorkPart = 45,

    // -------------------------------- Game Metadata Types ---------------------------------

    /// <summary>
    /// The metadata represents a video game franchise.
    /// </summary>
    /// <remarks>
    /// A game franchise is a collection of multiple related video games, which can be spread
    /// across different platforms and genres, but share common elements such as characters,
    /// settings, or storylines.
    /// Various game series can belong to the same franchise.
    /// </remarks>
    GameFranchise = 60,

    /// <summary>
    /// The metadata represents a video game series.
    /// </summary>
    /// <remarks>
    /// A game series is a set of video games that are directly related to each other,
    /// typically sharing the same title, characters, and storyline, and are usually released
    /// sequentially as part of a continuing narrative or gameplay experience.
    /// </remarks>
    GameSeries = 61,

    /// <summary>
    /// The metadata represents a single video game.
    /// </summary>
    /// <remarks>
    /// A game is an individual video game title that can be played on various platforms.
    /// This metadata type encompasses all releases of the game, including different editions,
    /// platform versions, and updates.
    /// </remarks>
    Game = 62,

    /// <summary>
    /// The metadata represents a game release.
    /// </summary>
    /// <remarks>
    /// A game release is a specific version of a video game that has been made available
    /// to the public. This can include different editions, platform-specific versions,
    /// and updates or patches that have been released after the initial launch.
    /// </remarks>
    GameRelease = 63,

    // ------------------------------- Person Metadata Types --------------------------------

    /// <summary>
    /// The metadata represents an individual person.
    /// </summary>
    /// <remarks>
    /// This is used both for actors, directors, and crew, but also for artists in music and authors in books.
    /// </remarks>
    Person = 90,

    /// <summary>
    /// The metadata represents a group of people, such as a band, a troupe, or a cast.
    /// </summary>
    Group = 95,

    // ------------------------------ Other Metadata Types ---------------------------------

    /// <summary>
    /// Collection metadata type.
    /// </summary>
    Collection = 100,

    /// <summary>
    /// Playlist metadata type.
    /// </summary>
    Playlist = 110,

    /// <summary>
    /// Playlists folder metadata type.
    /// </summary>
    PlaylistsFolder = 120,

    /// <summary>
    /// Trailer metadata type.
    /// </summary>
    Trailer = 200,

    /// <summary>
    /// Clip metadata type.
    /// </summary>
    Clip = 300,

    /// <summary>
    /// Behind-the-scenes extra metadata type.
    /// </summary>
    BehindTheScenes = 310,

    /// <summary>
    /// Deleted scene extra metadata type.
    /// </summary>
    DeletedScene = 320,

    /// <summary>
    /// Featurette extra metadata type.
    /// </summary>
    Featurette = 330,

    /// <summary>
    /// Interview extra metadata type.
    /// </summary>
    Interview = 340,

    /// <summary>
    /// Scene extra metadata type.
    /// </summary>
    Scene = 350,

    /// <summary>
    /// Short-form extra metadata type.
    /// </summary>
    ShortForm = 360,

    /// <summary>
    /// Other/uncategorized extra metadata type.
    /// </summary>
    ExtraOther = 399,

    /// <summary>
    /// Optimized version metadata type.
    /// </summary>
    OptimizedVersion = 500,

    /// <summary>
    /// User playlist item metadata type.
    /// </summary>
    UserPlaylistItem = 1000,
}
