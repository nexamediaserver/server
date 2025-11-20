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
    /// The default depth (in directory levels) for real-time filesystem watching.
    /// Directories beyond this depth are monitored via periodic polling instead.
    /// </summary>
    public const int DefaultWatcherDepth = 4;

    /// <summary>
    /// The default polling interval for monitoring directories beyond <see cref="WatcherDepth"/>.
    /// </summary>
    public static readonly TimeSpan DefaultWatcherPollingInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the preferred language code (BCP-47) for metadata lookup (titles, summaries, etc.).
    /// </summary>
    public string PreferredMetadataLanguage { get; set; } = "en";

    /// <summary>
    /// Gets or sets the ordered list of metadata agent identifiers to execute when refreshing metadata.
    /// </summary>
    public List<string> MetadataAgentOrder { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of metadata agent identifiers that are disabled for this library.
    /// Agents in this list will be skipped during metadata refresh even if present in <see cref="MetadataAgentOrder"/>.
    /// </summary>
    public List<string> DisabledMetadataAgents { get; set; } = [];

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

    /// <summary>
    /// Gets or sets a value indicating whether real-time filesystem watching is enabled for this library.
    /// When disabled, changes are only detected via scheduled scans.
    /// </summary>
    public bool WatcherEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the depth (in directory levels from the library root) to use real-time filesystem watching.
    /// Directories beyond this depth are monitored via periodic polling.
    /// Defaults to <see cref="DefaultWatcherDepth"/> (4 levels).
    /// </summary>
    /// <remarks>
    /// A value of 4 covers typical media structures like <c>Movies/Title (Year)/</c>
    /// or <c>TV/Show/Season X/Episode</c>. Increase for deeply nested structures.
    /// </remarks>
    public int WatcherDepth { get; set; } = DefaultWatcherDepth;

    /// <summary>
    /// Gets or sets the interval for polling directories beyond <see cref="WatcherDepth"/>.
    /// Only applies when <see cref="WatcherEnabled"/> is true.
    /// </summary>
    public TimeSpan WatcherPollingInterval { get; set; } = DefaultWatcherPollingInterval;

    /// <summary>
    /// Gets or sets the JSON-serialized hub configuration for the library's discover page.
    /// Contains enabled hub types and their display order.
    /// </summary>
    public string? DiscoverHubConfigurationJson { get; set; }
}
