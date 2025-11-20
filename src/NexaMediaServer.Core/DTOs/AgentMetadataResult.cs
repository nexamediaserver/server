// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

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
    /// Gets external identifiers provided by the agent (e.g., imdb, tmdb, tvdb).
    /// </summary>
    public Dictionary<string, string> ExternalIds { get; init; } = new();
}
