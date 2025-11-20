// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later
namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of relationship between metadata items.
/// </summary>
public enum RelationType
{
    /// <summary>
    /// One person metadata entry is an alias of another person entry.
    /// </summary>
    PersonAliasOfPerson = 10,

    /// <summary>
    /// Two person metadata entries collaborated on a work.
    /// </summary>
    PersonCollaboratedWithPerson = 11,

    /// <summary>
    /// One person metadata entry mentored another person entry.
    /// </summary>
    PersonMentoredPerson = 12,

    // ------------------------------ Person ↔ Group ------------------------------------

    /// <summary>
    /// A person metadata entry is a member of a group metadata entry.
    /// </summary>
    PersonMemberOfGroup = 50,

    /// <summary>
    /// A group metadata entry includes a person as a member.
    /// </summary>
    GroupHasMember = 51,

    /// <summary>
    /// A person metadata entry leads or fronts a group metadata entry.
    /// </summary>
    PersonLeadsGroup = 52,

    /// <summary>
    /// A group metadata entry is led by a person metadata entry.
    /// </summary>
    GroupLedByPerson = 53,

    /// <summary>
    /// A person metadata entry founded a group metadata entry.
    /// </summary>
    PersonFoundedGroup = 54,

    /// <summary>
    /// A group metadata entry was founded by a specific person metadata entry.
    /// </summary>
    GroupFoundedByPerson = 55,

    // ------------------------------ Generic Contributions ------------------------------

    /// <summary>
    /// A person metadata entry performs in a video metadata entry
    /// (movie, show, season, or episode).
    /// </summary>
    PersonPerformsInVideo = 100,

    /// <summary>
    /// A person metadata entry contributes as crew to a video metadata entry
    /// (movie, show, season, or episode).
    /// </summary>
    PersonContributesCrewToVideo = 101,

    /// <summary>
    /// A person metadata entry contributes music to a video metadata entry
    /// (score, soundtrack, theme, etc.).
    /// </summary>
    PersonContributesMusicToVideo = 102,

    /// <summary>
    /// A person metadata entry contributes to an audio metadata entry
    /// (track, recording, album release, or audio work).
    /// </summary>
    PersonContributesToAudio = 150,

    /// <summary>
    /// A person metadata entry contributes to a literary metadata entry
    /// (literary work, edition, edition item, or book series).
    /// </summary>
    PersonContributesToLiteraryWork = 200,

    /// <summary>
    /// A person metadata entry contributes to a game metadata entry.
    /// </summary>
    PersonContributesToGame = 240,

    /// <summary>
    /// A person metadata entry contributes to a photo or photo collection
    /// metadata entry (photo, photo album, or picture set).
    /// </summary>
    PersonContributesToPhoto = 280,

    /// <summary>
    /// A person metadata entry contributes to a collection or playlist
    /// metadata entry.
    /// </summary>
    PersonContributesToCollectionOrPlaylist = 320,

    /// <summary>
    /// A group metadata entry contributes to an audio metadata entry
    /// (track, recording, or album release).
    /// </summary>
    GroupContributesToAudio = 300,

    /// <summary>
    /// A group metadata entry contributes to a video metadata entry
    /// (movie, show, season, or episode).
    /// </summary>
    GroupContributesToVideo = 301,

    /// <summary>
    /// A group metadata entry contributes to a game metadata entry.
    /// </summary>
    GroupContributesToGame = 303,

    // ------------------------------ Video ↔ Video -------------------------------------

    /// <summary>
    /// A movie metadata entry is a sequel to another movie metadata entry.
    /// </summary>
    MovieSequelToMovie = 400,

    /// <summary>
    /// A movie metadata entry is a prequel to another movie metadata entry.
    /// </summary>
    MoviePrequelToMovie = 401,

    /// <summary>
    /// A movie metadata entry is a remake of another movie metadata entry.
    /// </summary>
    MovieRemakeOfMovie = 402,

    /// <summary>
    /// A movie metadata entry is a spin-off from another movie metadata entry.
    /// </summary>
    MovieSpinOffFromMovie = 403,

    /// <summary>
    /// A movie metadata entry represents an alternate cut of another movie metadata entry.
    /// </summary>
    MovieAlternateCutOfMovie = 404,

    /// <summary>
    /// Two movie metadata entries share the same cinematic universe.
    /// </summary>
    MovieSharesUniverseWithMovie = 405,

    /// <summary>
    /// A show metadata entry is a spin-off from another show metadata entry.
    /// </summary>
    ShowSpinOffFromShow = 406,

    /// <summary>
    /// Two show metadata entries share the same universe or continuity.
    /// </summary>
    ShowSharesUniverseWithShow = 407,

    /// <summary>
    /// Two show metadata entries feature a crossover event.
    /// </summary>
    ShowCrossoverWithShow = 408,

    /// <summary>
    /// A season metadata entry is a recut or re-release of another season metadata entry.
    /// </summary>
    SeasonRecutFromSeason = 409,

    /// <summary>
    /// Two episode metadata entries form a crossover story.
    /// </summary>
    EpisodeCrossoverWithEpisode = 410,

    /// <summary>
    /// An episode metadata entry is a remake of another episode metadata entry.
    /// </summary>
    EpisodeRemakeOfEpisode = 411,

    /// <summary>
    /// An episode metadata entry is a clip show that reuses footage from another episode metadata entry.
    /// </summary>
    EpisodeClipShowOfEpisode = 412,

    /// <summary>
    /// A trailer metadata entry promotes another metadata entry (movie, show, season, or episode).
    /// </summary>
    TrailerPromotesMetadata = 413,

    /// <summary>
    /// An optimized version metadata entry references the source metadata item it was generated from.
    /// </summary>
    OptimizedVersionOfMetadata = 414,

    /// <summary>
    /// A clip metadata entry supplements another metadata entry (movie, show, season, or episode).
    /// </summary>
    ClipSupplementsMetadata = 415,

    // ------------------------------ Audio ↔ Audio -------------------------------------

    /// <summary>
    /// A track metadata entry is a remix of another track metadata entry.
    /// </summary>
    TrackRemixOfTrack = 450,

    /// <summary>
    /// A track metadata entry is a cover of another track metadata entry.
    /// </summary>
    TrackCoverOfTrack = 451,

    /// <summary>
    /// A track metadata entry samples another track metadata entry.
    /// </summary>
    TrackSamplesTrack = 452,

    /// <summary>
    /// A track metadata entry is a live performance variant of another track metadata entry.
    /// </summary>
    TrackLiveVersionOfTrack = 453,

    /// <summary>
    /// A track metadata entry is an acoustic version of another track metadata entry.
    /// </summary>
    TrackAcousticVersionOfTrack = 454,

    /// <summary>
    /// A track metadata entry is an instrumental version of another track metadata entry.
    /// </summary>
    TrackInstrumentalVersionOfTrack = 455,

    /// <summary>
    /// A recording metadata entry is a remaster of another recording metadata entry.
    /// </summary>
    RecordingRemasterOfRecording = 456,

    /// <summary>
    /// An album release metadata entry is a reissue of another album release metadata entry.
    /// </summary>
    AlbumReleaseReissueOfAlbumRelease = 457,

    /// <summary>
    /// An album release group metadata entry continues another album release group metadata entry.
    /// </summary>
    AlbumReleaseGroupContinuationOfGroup = 458,

    /// <summary>
    /// A track metadata entry is a mashup built from other track metadata entries.
    /// </summary>
    TrackMashupOfTracks = 459,

    // ------------------------------ Literary ↔ Literary --------------------------------

    /// <summary>
    /// An edition metadata entry is a translation of another edition metadata entry.
    /// </summary>
    EditionTranslationOfEdition = 500,

    /// <summary>
    /// An edition metadata entry is a revision of another edition metadata entry.
    /// </summary>
    EditionRevisionOfEdition = 501,

    /// <summary>
    /// An edition metadata entry compiles edition item metadata entries from another edition.
    /// </summary>
    EditionCompilationOfEditionItems = 502,

    /// <summary>
    /// A book series metadata entry is a spin-off of another book series metadata entry.
    /// </summary>
    BookSeriesSpinOffFromSeries = 503,

    /// <summary>
    /// A literary work metadata entry is a sequel to another literary work metadata entry.
    /// </summary>
    LiteraryWorkSequelToLiteraryWork = 504,

    /// <summary>
    /// A literary work metadata entry is a prequel to another literary work metadata entry.
    /// </summary>
    LiteraryWorkPrequelToLiteraryWork = 505,

    /// <summary>
    /// Two literary work metadata entries share the same continuity or universe.
    /// </summary>
    LiteraryWorkSharesUniverseWithWork = 506,

    /// <summary>
    /// An edition metadata entry adapts a literary work metadata entry (e.g., graphic adaptation).
    /// </summary>
    EditionAdaptedFromLiteraryWork = 507,

    // ------------------------------ Game ↔ Game ---------------------------------------

    /// <summary>
    /// A game metadata entry is a sequel to another game metadata entry.
    /// </summary>
    GameSequelToGame = 550,

    /// <summary>
    /// A game metadata entry is a prequel to another game metadata entry.
    /// </summary>
    GamePrequelToGame = 551,

    /// <summary>
    /// A game metadata entry is a spin-off from another game metadata entry.
    /// </summary>
    GameSpinOffFromGame = 552,

    /// <summary>
    /// A game metadata entry is a remaster of another game metadata entry.
    /// </summary>
    GameRemasterOfGame = 553,

    /// <summary>
    /// A game metadata entry is a remake of another game metadata entry.
    /// </summary>
    GameRemakeOfGame = 554,

    /// <summary>
    /// A game metadata entry is an expansion for another game metadata entry.
    /// </summary>
    GameExpansionOfGame = 555,

    /// <summary>
    /// A game metadata entry represents downloadable content for another game metadata entry.
    /// </summary>
    GameDlcOfGame = 556,

    /// <summary>
    /// A game release metadata entry is a port of another game metadata entry.
    /// </summary>
    GameReleasePortOfGame = 557,

    /// <summary>
    /// Two game metadata entries share the same franchise or narrative universe.
    /// </summary>
    GameSharesUniverseWithSeries = 558,

    // ------------------------------ Photo ↔ Photo -------------------------------------

    /// <summary>
    /// A photo metadata entry is derived from another photo metadata entry (e.g., cropped or retouched).
    /// </summary>
    PhotoDerivedFromPhoto = 600,

    /// <summary>
    /// A photo metadata entry is a colorized version of another photo metadata entry.
    /// </summary>
    PhotoColorizedFromPhoto = 601,

    /// <summary>
    /// A photo album metadata entry continues or extends another photo album metadata entry.
    /// </summary>
    PhotoAlbumContinuationOfAlbum = 602,

    /// <summary>
    /// Two picture set metadata entries explore the same theme or event.
    /// </summary>
    PictureSetSharesThemeWithPictureSet = 603,

    // ------------------------------ Cross-Media ----------------------------------------

    /// <summary>
    /// A movie metadata entry adapts a literary work metadata entry.
    /// </summary>
    MovieAdaptedFromLiteraryWork = 650,

    /// <summary>
    /// A show metadata entry adapts a literary work metadata entry.
    /// </summary>
    ShowAdaptedFromLiteraryWork = 651,

    /// <summary>
    /// A game metadata entry adapts a literary work metadata entry.
    /// </summary>
    GameAdaptedFromLiteraryWork = 652,

    /// <summary>
    /// A movie metadata entry adapts a game metadata entry.
    /// </summary>
    MovieAdaptedFromGame = 653,

    /// <summary>
    /// A show metadata entry adapts a game metadata entry.
    /// </summary>
    ShowAdaptedFromGame = 654,

    /// <summary>
    /// A game metadata entry adapts a show metadata entry.
    /// </summary>
    GameAdaptedFromShow = 655,

    /// <summary>
    /// A game metadata entry adapts a movie metadata entry.
    /// </summary>
    GameAdaptedFromMovie = 656,

    /// <summary>
    /// A literary work metadata entry novelizes a movie metadata entry.
    /// </summary>
    LiteraryWorkNovelizationOfMovie = 657,

    /// <summary>
    /// A literary work metadata entry novelizes a show metadata entry.
    /// </summary>
    LiteraryWorkNovelizationOfShow = 658,

    /// <summary>
    /// An audio work metadata entry is inspired by a literary work metadata entry.
    /// </summary>
    AudioWorkInspiredByLiteraryWork = 659,

    /// <summary>
    /// An audio work metadata entry is inspired by a show metadata entry.
    /// </summary>
    AudioWorkInspiredByShow = 660,

    /// <summary>
    /// An audio work metadata entry is inspired by a game metadata entry.
    /// </summary>
    AudioWorkInspiredByGame = 661,

    /// <summary>
    /// A track metadata entry is recorded as a tribute to a person metadata entry.
    /// </summary>
    TrackTributeToPerson = 662,

    /// <summary>
    /// A documentary metadata entry (movie or show) centers around a person metadata entry.
    /// </summary>
    DocumentaryAboutPerson = 663,

    /// <summary>
    /// A documentary metadata entry centers around a group metadata entry.
    /// </summary>
    DocumentaryAboutGroup = 664,

    // -------------------------- Collections & Playlists --------------------------------

    /// <summary>
    /// A playlist metadata entry curates items from a collection metadata entry.
    /// </summary>
    PlaylistCuratesCollection = 700,

    /// <summary>
    /// A playlist metadata entry is derived from another playlist metadata entry.
    /// </summary>
    PlaylistBasedOnPlaylist = 701,

    /// <summary>
    /// A playlist metadata entry is seeded directly from a specific track metadata entry.
    /// </summary>
    PlaylistDerivedFromTrack = 702,

    /// <summary>
    /// A collection metadata entry aggregates one or more playlist metadata entries.
    /// </summary>
    CollectionAggregatesPlaylist = 703,

    /// <summary>
    /// Two collection metadata entries reference or mirror each other.
    /// </summary>
    CollectionReferencesCollection = 704,

    /// <summary>
    /// A playlists folder metadata entry organizes a playlist metadata entry.
    /// </summary>
    PlaylistFolderOrganizesPlaylist = 705,

    /// <summary>
    /// A user playlist item metadata entry references another metadata entry (track, movie, etc.).
    /// </summary>
    UserPlaylistItemReferencesMetadata = 706,

    /// <summary>
    /// A collection metadata entry honors or is dedicated to a person metadata entry.
    /// </summary>
    CollectionHonorsPerson = 707,
}
