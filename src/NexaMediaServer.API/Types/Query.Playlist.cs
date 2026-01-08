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
/// Playlist-related GraphQL queries for retrieving playlist chunks.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Retrieves a chunk of playlist items.
    /// </summary>
    /// <param name="input">The chunk request parameters.</param>
    /// <param name="playlistService">The playlist service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The playlist chunk payload.</returns>
    [Authorize]
    public static async Task<PlaylistChunkPayload> GetPlaylistChunkAsync(
        PlaylistChunkInput input,
        [Service] IPlaylistService playlistService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaylistInputError("Playlist chunk input is required.");
        }

        int sessionId = await ResolvePlaylistSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        var response = await playlistService.GetPlaylistChunkAsync(
            sessionId,
            new PlaylistChunkRequest
            {
                PlaylistGeneratorId = input.PlaylistGeneratorId,
                StartIndex = input.StartIndex,
                Limit = input.Limit,
            },
            cancellationToken
        );

        return PlaylistChunkPayload.FromDto(response);
    }

    private static GraphQLException CreatePlaylistInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder.New().SetMessage(message).SetCode("PLAYLIST_INPUT_INVALID").Build()
        );
    }

    private static GraphQLException CreatePlaylistSessionError()
    {
        return new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage("An active session is required for playlist operations.")
                .SetCode("PLAYLIST_SESSION_REQUIRED")
                .Build()
        );
    }

    private static async Task<int> ResolvePlaylistSessionIdAsync(
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
            throw CreatePlaylistSessionError();
        }

        if (!Guid.TryParse(sessionClaim, out Guid publicSessionId))
        {
            throw CreatePlaylistSessionError();
        }

        var session = await sessionService.ValidateSessionAsync(publicSessionId, cancellationToken);
        if (session is null)
        {
            throw CreatePlaylistSessionError();
        }

        return session.Id;
    }
}
