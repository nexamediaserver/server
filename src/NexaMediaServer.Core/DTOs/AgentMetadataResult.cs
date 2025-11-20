// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents metadata returned by a metadata agent for a specific item.
/// The shape is intentionally simple and optional; agents may populate only known fields.
/// </summary>
public sealed class AgentMetadataResult
{
    /// <summary>
    /// Gets the localized display title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the original title as provided by the source.
    /// </summary>
    public string? OriginalTitle { get; init; }

    /// <summary>
    /// Gets the plot summary/overview.
    /// </summary>
    public string? Overview { get; init; }

    /// <summary>
    /// Gets the release or air date when available.
    /// </summary>
    public DateTime? ReleaseDate { get; init; }

    /// <summary>
    /// Gets the content rating identifier (e.g., "PG-13", "R", "TV-MA").
    /// </summary>
    public string? ContentRating { get; init; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code associated with the content rating (e.g., "US", "UK", "JP").
    /// </summary>
    public string? ContentRatingCountryCode { get; init; }

    /// <summary>
    /// Gets external identifiers provided by the agent (e.g., imdb, tmdb, tvdb).
    /// </summary>
    public Dictionary<string, string> ExternalIds { get; init; } = new();

    /// <summary>
    /// Gets optional artwork locations provided by the agent (absolute file paths or URLs) keyed by artwork kind.
    /// </summary>
    public Dictionary<ArtworkKind, string> Artwork { get; init; } = new();

    /// <summary>
    /// Gets optional person contributions provided by the agent.
    /// </summary>
    public IReadOnlyList<PersonCredit> People { get; init; } = Array.Empty<PersonCredit>();

    /// <summary>
    /// Gets optional group contributions provided by the agent.
    /// </summary>
    public IReadOnlyList<GroupCredit> Groups { get; init; } = Array.Empty<GroupCredit>();

    /// <summary>
    /// Gets the list of genres for this metadata item.
    /// </summary>
    /// <remarks>
    /// Genre names will be normalized using configured mappings and deduplicated.
    /// </remarks>
    public List<string> Genres { get; init; } = [];

    /// <summary>
    /// Gets the list of tags for this metadata item.
    /// </summary>
    /// <remarks>
    /// Tags will be filtered through moderation (allowlist/blocklist) before being applied.
    /// </remarks>
    public List<string> Tags { get; init; } = [];
}
