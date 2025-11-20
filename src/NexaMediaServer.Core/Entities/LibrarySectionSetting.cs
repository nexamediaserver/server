// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents the collection of configurable settings for a single <see cref="LibrarySection"/>.
/// </summary>
public class LibrarySectionSetting
{
    /// <summary>
    /// Gets or sets the preferred language code (BCP-47) for metadata lookup (titles, summaries, etc.).
    /// </summary>
    public string PreferredMetadataLanguage { get; set; } = "en";

    /// <summary>
    /// Gets or sets the ordered list of metadata agent identifiers to execute when refreshing metadata.
    /// </summary>
    public List<string> MetadataAgentOrder { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether seasons should be hidden for single-season TV series.
    /// Applies only when <see cref="LibrarySection.Type"/> is <see cref="LibraryType.TVShows"/> or other episodic types.
    /// </summary>
    public bool HideSeasonsForSingleSeasonSeries { get; set; } = true;

    /// <summary>
    /// Gets or sets the episode sort order preference for episodic libraries.
    /// </summary>
    public EpisodeSortOrder EpisodeSortOrder { get; set; } = EpisodeSortOrder.AirDate;

    /// <summary>
    /// Gets or sets the preferred audio language codes (ordered by priority) for video playback.
    /// Only relevant for video-based libraries (Movies, TVShows, MusicVideos, HomeVideos).
    /// </summary>
    public List<string> PreferredAudioLanguages { get; set; } = [];

    /// <summary>
    /// Gets or sets the preferred subtitle language codes (ordered by priority) for video playback.
    /// Only relevant for video-based libraries.
    /// </summary>
    public List<string> PreferredSubtitleLanguages { get; set; } = [];

    /// <summary>
    /// Gets or sets the metadata agent specific settings. Key is the agent identifier.
    /// Values contain arbitrary key/value pairs (string to string) for flexible configuration.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> MetadataAgentSettings { get; set; } =
        new();
}
