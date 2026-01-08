// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using HotChocolate.Authorization;

using Microsoft.AspNetCore.Http;

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Core.Services.Authentication;

using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Playlist-related GraphQL mutations for navigation and mode changes.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Navigates to the next item in the playlist.
    /// </summary>
    /// <param name="input">The navigation input.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation payload with the next item.</returns>
    [Authorize]
    public static async Task<PlaylistNavigatePayload> PlaylistNextAsync(
        PlaylistNavigateInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistMutationInputError("Playlist navigate input is required.");
        }

        int sessionId = await ResolvePlaylistMutationSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        var item = await playlistService.GetNextItemAsync(
            sessionId,
            input.PlaylistGeneratorId,
            cancellationToken
        );

        // Get updated playlist state
        var chunk = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = 0,
                Limit = 1,
            },
            cancellationToken
        );

        return new PlaylistNavigatePayload
        {
            Success = item != null,
            CurrentItem = item != null ? PlaylistItemPayload.FromDto(item) : null,
            Shuffle = chunk.Shuffle,
            Repeat = chunk.Repeat,
            CurrentIndex = chunk.CurrentIndex,
            TotalCount = chunk.TotalCount,
        };
    }

    /// <summary>
    /// Navigates to the previous item in the playlist.
    /// </summary>
    /// <param name="input">The navigation input.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation payload with the previous item.</returns>
    [Authorize]
    public static async Task<PlaylistNavigatePayload> PlaylistPreviousAsync(
        PlaylistNavigateInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistMutationInputError("Playlist navigate input is required.");
        }

        int sessionId = await ResolvePlaylistMutationSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        var item = await playlistService.GetPreviousItemAsync(
            sessionId,
            input.PlaylistGeneratorId,
            cancellationToken
        );

        // Get updated playlist state
        var chunk = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = 0,
                Limit = 1,
            },
            cancellationToken
        );

        return new PlaylistNavigatePayload
        {
            Success = item != null,
            CurrentItem = item != null ? PlaylistItemPayload.FromDto(item) : null,
            Shuffle = chunk.Shuffle,
            Repeat = chunk.Repeat,
            CurrentIndex = chunk.CurrentIndex,
            TotalCount = chunk.TotalCount,
        };
    }

    /// <summary>
    /// Jumps to a specific index in the playlist.
    /// </summary>
    /// <param name="input">The jump input with target index.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation payload with the item at the specified index.</returns>
    [Authorize]
    public static async Task<PlaylistNavigatePayload> PlaylistJumpAsync(
        PlaylistJumpInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistMutationInputError("Playlist jump input is required.");
        }

        int sessionId = await ResolvePlaylistMutationSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        var item = await playlistService.JumpToIndexAsync(
            sessionId,
            input.PlaylistGeneratorId,
            input.Index,
            cancellationToken
        );

        // Get updated playlist state
        var chunk = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = 0,
                Limit = 1,
            },
            cancellationToken
        );

        return new PlaylistNavigatePayload
        {
            Success = item != null,
            CurrentItem = item != null ? PlaylistItemPayload.FromDto(item) : null,
            Shuffle = chunk.Shuffle,
            Repeat = chunk.Repeat,
            CurrentIndex = chunk.CurrentIndex,
            TotalCount = chunk.TotalCount,
        };
    }

    /// <summary>
    /// Sets shuffle mode on the playlist.
    /// </summary>
    /// <param name="input">The mode input with enabled flag.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation payload with updated state.</returns>
    [Authorize]
    public static async Task<PlaylistNavigatePayload> PlaylistSetShuffleAsync(
        PlaylistModeInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistMutationInputError("Playlist mode input is required.");
        }

        int sessionId = await ResolvePlaylistMutationSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        bool success = await playlistService.SetShuffleAsync(
            sessionId,
            input.PlaylistGeneratorId,
            input.Enabled,
            cancellationToken
        );

        // Get updated playlist state
        var chunk = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = 0,
                Limit = 1,
            },
            cancellationToken
        );

        var currentItem =
            chunk.Items.Count > 0 && chunk.CurrentIndex < chunk.Items.Count
                ? PlaylistItemPayload.FromDto(chunk.Items[0])
                : null;

        return new PlaylistNavigatePayload
        {
            Success = success,
            CurrentItem = currentItem,
            Shuffle = chunk.Shuffle,
            Repeat = chunk.Repeat,
            CurrentIndex = chunk.CurrentIndex,
            TotalCount = chunk.TotalCount,
        };
    }

    /// <summary>
    /// Sets repeat mode on the playlist.
    /// </summary>
    /// <param name="input">The mode input with enabled flag.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation payload with updated state.</returns>
    [Authorize]
    public static async Task<PlaylistNavigatePayload> PlaylistSetRepeatAsync(
        PlaylistModeInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistMutationInputError("Playlist mode input is required.");
        }

        int sessionId = await ResolvePlaylistMutationSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        bool success = await playlistService.SetRepeatAsync(
            sessionId,
            input.PlaylistGeneratorId,
            input.Enabled,
            cancellationToken
        );

        // Get updated playlist state
        var chunk = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = 0,
                Limit = 1,
            },
            cancellationToken
        );

        var currentItem =
            chunk.Items.Count > 0 && chunk.CurrentIndex < chunk.Items.Count
                ? PlaylistItemPayload.FromDto(chunk.Items[0])
                : null;

        return new PlaylistNavigatePayload
        {
            Success = success,
            CurrentItem = currentItem,
            Shuffle = chunk.Shuffle,
            Repeat = chunk.Repeat,
            CurrentIndex = chunk.CurrentIndex,
            TotalCount = chunk.TotalCount,
        };
    }

    private static GraphQLException CreatePlaylistMutationInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder.New().SetMessage(message).SetCode("PLAYLIST_INPUT_INVALID").Build()
        );
    }

    private static GraphQLException CreatePlaylistMutationSessionError()
    {
        return new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage("An active session is required for playlist operations.")
                .SetCode("PLAYLIST_SESSION_REQUIRED")
                .Build()
        );
    }

    private static async Task<int> ResolvePlaylistMutationSessionIdAsync(
        IHttpContextAccessor httpContextAccessor,
        ISessionService sessionService,
        CancellationToken cancellationToken
    )
    {
        string? sessionClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypeConstants.SessionId
        );

        if (string.IsNullOrWhiteSpace(sessionClaim))
        {
            throw CreatePlaylistMutationSessionError();
        }

        if (!Guid.TryParse(sessionClaim, out Guid publicSessionId))
        {
            throw CreatePlaylistMutationSessionError();
        }

        var session = await sessionService.ValidateSessionAsync(publicSessionId, cancellationToken);
        if (session is null)
        {
            throw CreatePlaylistMutationSessionError();
        }

        return session.Id;
    }
}
