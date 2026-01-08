// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload containing a chunk of playlist items.
/// </summary>
public sealed class PlaylistChunkPayload
{
    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the items in this chunk.
    /// </summary>
    public List<PlaylistItemPayload> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the current cursor position (0-based index of the currently playing item).
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// Gets or sets the total number of items in the playlist.
    /// -1 indicates the total is unknown (e.g., for infinite/dynamic playlists).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there are more items available after this chunk.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled.
    /// </summary>
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode is enabled.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Creates a payload from a DTO.
    /// </summary>
    /// <param name="response">The playlist chunk response DTO.</param>
    /// <returns>The GraphQL payload.</returns>
    public static PlaylistChunkPayload FromDto(PlaylistChunkResponse response)
    {
        return new PlaylistChunkPayload
        {
            PlaylistGeneratorId = response.PlaylistGeneratorId,
            Items = response.Items.Select(PlaylistItemPayload.FromDto).ToList(),
            CurrentIndex = response.CurrentIndex,
            TotalCount = response.TotalCount,
            HasMore = response.HasMore,
            Shuffle = response.Shuffle,
            Repeat = response.Repeat,
        };
    }
}
