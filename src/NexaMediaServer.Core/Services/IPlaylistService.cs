// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for managing playback playlists including generation, navigation, and chunk retrieval.
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Creates and initializes a playlist generator for a playback session.
    /// </summary>
    /// <param name="playbackSessionId">The database ID of the playback session.</param>
    /// <param name="seed">The playlist seed defining how items are generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created playlist generator.</returns>
    Task<PlaylistGenerator> CreatePlaylistAsync(
        int playbackSessionId,
        PlaylistSeed seed,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Retrieves a chunk of playlist items.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="request">The chunk request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The playlist chunk with items and navigation info.</returns>
    Task<PlaylistChunkResponse> GetPlaylistChunkAsync(
        int sessionId,
        PlaylistChunkRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets the next item in the playlist and advances the cursor.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="playlistGeneratorId">The playlist generator public identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next playlist item or null if at the end of the playlist.</returns>
    Task<PlaylistItem?> GetNextItemAsync(
        int sessionId,
        Guid playlistGeneratorId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets the previous item in the playlist and moves the cursor back.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="playlistGeneratorId">The playlist generator public identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The previous playlist item or null if at the start of the playlist.</returns>
    Task<PlaylistItem?> GetPreviousItemAsync(
        int sessionId,
        Guid playlistGeneratorId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Jumps to a specific index in the playlist.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="playlistGeneratorId">The playlist generator public identifier.</param>
    /// <param name="index">The 0-based index to jump to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The playlist item at the specified index.</returns>
    Task<PlaylistItem?> JumpToIndexAsync(
        int sessionId,
        Guid playlistGeneratorId,
        int index,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Toggles shuffle mode for a playlist and re-shuffles if enabling.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="playlistGeneratorId">The playlist generator public identifier.</param>
    /// <param name="enabled">Whether shuffle should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the operation succeeded.</returns>
    Task<bool> SetShuffleAsync(
        int sessionId,
        Guid playlistGeneratorId,
        bool enabled,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sets repeat mode for a playlist.
    /// </summary>
    /// <param name="sessionId">The user session identifier for authorization.</param>
    /// <param name="playlistGeneratorId">The playlist generator public identifier.</param>
    /// <param name="enabled">Whether repeat should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the operation succeeded.</returns>
    Task<bool> SetRepeatAsync(
        int sessionId,
        Guid playlistGeneratorId,
        bool enabled,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Materializes items into the playlist generator if needed.
    /// Called internally to ensure items exist before retrieval.
    /// </summary>
    /// <param name="generator">The playlist generator to materialize items for.</param>
    /// <param name="upToIndex">The index up to which items should be materialized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of items materialized.</returns>
    Task<int> MaterializeItemsAsync(
        PlaylistGenerator generator,
        int upToIndex,
        CancellationToken cancellationToken
    );
}
