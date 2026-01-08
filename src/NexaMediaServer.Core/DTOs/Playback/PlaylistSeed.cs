// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents a playlist seed that defines how a playlist is generated.
/// This is serialized to JSON and stored in <see cref="Entities.PlaylistGenerator.SeedJson"/>.
/// </summary>
public class PlaylistSeed
{
    /// <summary>
    /// Gets or sets the type of playlist seed (e.g., "single", "album", "show", "library", "collection").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "single";

    /// <summary>
    /// Gets or sets the originator metadata item ID that defines the source of this playlist.
    /// For single items, this is the item itself. For albums/shows, this is the parent container.
    /// </summary>
    [JsonPropertyName("originatorId")]
    public int? OriginatorId { get; set; }

    /// <summary>
    /// Gets or sets the starting index within the originator's children (0-based).
    /// Used when starting playback from a specific track in an album or episode in a show.
    /// </summary>
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the list of explicit metadata item IDs in this playlist.
    /// Used for custom playlists or when items are explicitly ordered.
    /// </summary>
    [JsonPropertyName("items")]
    public List<int>? Items { get; set; }

    /// <summary>
    /// Gets or sets the library section ID for library-based playlists.
    /// </summary>
    [JsonPropertyName("librarySectionId")]
    public int? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the filter JSON for smart/dynamic playlists.
    /// </summary>
    [JsonPropertyName("filter")]
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the sort expression for ordered playlists.
    /// </summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle is enabled at playlist creation time.
    /// </summary>
    [JsonPropertyName("shuffle")]
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode is enabled at playlist creation time.
    /// </summary>
    [JsonPropertyName("repeat")]
    public bool Repeat { get; set; }
}
