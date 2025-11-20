// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL representation of per-library settings.
/// </summary>
[GraphQLName("LibrarySectionSettings")]
public sealed class LibrarySectionSettings
{
    /// <summary>
    /// Gets the preferred metadata language (BCP-47), e.g. "en", "de-DE".
    /// </summary>
    public string PreferredMetadataLanguage { get; init; } = "en";

    /// <summary>
    /// Gets the ordered list of metadata agent identifiers to use.
    /// </summary>
    public List<string> MetadataAgentOrder { get; init; } = [];

    /// <summary>
    /// Gets the list of metadata agent identifiers that are disabled for this library.
    /// </summary>
    public List<string> DisabledMetadataAgents { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether to hide seasons for single-season series.
    /// </summary>
    public bool HideSeasonsForSingleSeasonSeries { get; init; }

    /// <summary>
    /// Gets the episode sort order preference for episodic content.
    /// </summary>
    public EpisodeSortOrder EpisodeSortOrder { get; init; }

    /// <summary>
    /// Gets the preferred audio languages (ordered).
    /// </summary>
    public List<string> PreferredAudioLanguages { get; init; } = [];

    /// <summary>
    /// Gets the preferred subtitle languages (ordered).
    /// </summary>
    public List<string> PreferredSubtitleLanguages { get; init; } = [];

    /// <summary>
    /// Gets the map of metadata agent specific settings: agentId -> (key -> value).
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> MetadataAgentSettings { get; init; } =
        new();
}
