// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using HotChocolate.Authorization;

using Microsoft.AspNetCore.Http;

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Core.Services.Authentication;
using NexaMediaServer.Infrastructure.Services;

using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;
using CoreMetadataItem = NexaMediaServer.Core.Entities.MetadataItem;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Playback mutations for session-bound streaming orchestration.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Starts playback for a metadata item.
    /// </summary>
    /// <param name="input">Playback start details.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="metadataService">Metadata lookup service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created playback session details.</returns>
    [Authorize]
    public static async Task<PlaybackStartPayload> StartPlaybackAsync(
        PlaybackStartInput input,
        [Service] IPlaybackService playbackService,
        [Service] IMetadataService metadataService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback start input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        if (input.Capability != null)
        {
            await playbackService.UpsertCapabilityProfileAsync(
                sessionId,
                input.Capability.ToDto(),
                cancellationToken
            );
        }

        var metadata = await ResolveMetadataEntityAsync(input.ItemId, metadataService);

        // Resolve originator metadata item if provided
        int? originatorMetadataItemId = null;
        if (input.OriginatorId.HasValue)
        {
            var originator = await metadataService.GetByUuidAsync(input.OriginatorId.Value);
            originatorMetadataItemId = originator?.Id;
        }

        PlaybackStartResponse response = await playbackService.StartPlaybackAsync(
            sessionId,
            new PlaybackStartRequest
            {
                MetadataItemId = metadata.Id,
                OriginatorMetadataItemId = originatorMetadataItemId,
                PlaylistType = input.PlaylistType,
                Shuffle = input.Shuffle,
                Repeat = input.Repeat,
                Originator = input.Originator,
                ContextJson = input.ContextJson,
                CapabilityProfileVersion = input.CapabilityProfileVersion,
            },
            cancellationToken
        );

        bool capabilityMismatch =
            input.CapabilityProfileVersion.HasValue
            && input.CapabilityProfileVersion.Value != response.CapabilityProfileVersion;

        return new PlaybackStartPayload(response, capabilityMismatch);
    }

    /// <summary>
    /// Records a playback heartbeat for the active session.
    /// </summary>
    /// <param name="input">Heartbeat payload.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heartbeat acknowledgement payload.</returns>
    [Authorize]
    public static async Task<PlaybackHeartbeatPayload> PlaybackHeartbeatAsync(
        PlaybackHeartbeatInput input,
        [Service] IPlaybackService playbackService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback heartbeat input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        if (input.Capability != null)
        {
            await playbackService.UpsertCapabilityProfileAsync(
                sessionId,
                input.Capability.ToDto(),
                cancellationToken
            );
        }

        // Service now returns capability version, eliminating redundant DB query
        var response = await playbackService.HeartbeatAsync(
            sessionId,
            new PlaybackHeartbeatRequest
            {
                PlaybackSessionId = input.PlaybackSessionId,
                PlayheadMs = input.PlayheadMs,
                State = input.State,
                MediaPartId = input.MediaPartId,
            },
            cancellationToken
        );

        bool capabilityMismatch =
            input.CapabilityProfileVersion.HasValue
            && input.CapabilityProfileVersion.Value != response.CapabilityProfileVersion;

        return new PlaybackHeartbeatPayload(
            input.PlaybackSessionId,
            response.CapabilityProfileVersion,
            capabilityMismatch
        );
    }

    /// <summary>
    /// Requests a playback decision for the current session.
    /// </summary>
    /// <param name="input">Decision payload.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="metadataService">Metadata lookup service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decision payload.</returns>
    [Authorize]
    public static async Task<PlaybackDecisionPayload> DecidePlaybackAsync(
        PlaybackDecisionInput input,
        [Service] IPlaybackService playbackService,
        [Service] IMetadataService metadataService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback decision input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        if (input.Capability != null)
        {
            await playbackService.UpsertCapabilityProfileAsync(
                sessionId,
                input.Capability.ToDto(),
                cancellationToken
            );
        }

        var metadata = await ResolveMetadataEntityAsync(input.CurrentItemId, metadataService);

        // Service now returns all needed data including UUID and capability version
        PlaybackDecisionResponse response = await playbackService.DecideAsync(
            sessionId,
            new PlaybackDecisionRequest
            {
                PlaybackSessionId = input.PlaybackSessionId,
                CurrentMetadataItemId = metadata.Id,
                Status = input.Status,
                ProgressMs = input.ProgressMs,
                JumpIndex = input.JumpIndex,
            },
            cancellationToken
        );

        bool capabilityMismatch =
            input.CapabilityProfileVersion.HasValue
            && input.CapabilityProfileVersion.Value != response.CapabilityProfileVersion;

        return new PlaybackDecisionPayload(
            response,
            response.NextMetadataItemUuid,
            response.CapabilityProfileVersion,
            capabilityMismatch
        );
    }

    /// <summary>
    /// Attempts to resume a playback session by identifier.
    /// </summary>
    /// <param name="input">Resume payload.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resume details for the session.</returns>
    [Authorize]
    public static async Task<PlaybackResumePayload> ResumePlaybackAsync(
        PlaybackResumeInput input,
        [Service] IPlaybackService playbackService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback resume input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        if (input.Capability != null)
        {
            await playbackService.UpsertCapabilityProfileAsync(
                sessionId,
                input.Capability.ToDto(),
                cancellationToken
            );
        }

        // Service now returns all needed data eliminating redundant DB queries
        var response = await playbackService.ResumeAsync(
            sessionId,
            input.PlaybackSessionId,
            cancellationToken
        );

        if (response is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback session not found or expired.");
        }

        if (response.CurrentMetadataItemUuid == Guid.Empty)
        {
            throw CreatePlaybackGraphQLInputError(
                "Playback session metadata could not be resolved."
            );
        }

        bool capabilityMismatch =
            input.CapabilityProfileVersion.HasValue
            && input.CapabilityProfileVersion.Value != response.CapabilityProfileVersion;

        return new PlaybackResumePayload(
            response.Session,
            response.CurrentMetadataItemUuid,
            response.PlaylistGeneratorId,
            response.CapabilityProfileVersion,
            capabilityMismatch,
            response.StreamPlanJson,
            response.PlaybackUrl,
            response.TrickplayUrl,
            response.DurationMs,
            response.PlayheadMs,
            response.State
        );
    }

    /// <summary>
    /// Stops an active playback session and cleans up associated resources.
    /// </summary>
    /// <param name="input">Stop payload.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Acknowledgement payload.</returns>
    [Authorize]
    public static async Task<PlaybackStopPayload> StopPlaybackAsync(
        PlaybackStopInput input,
        [Service] IPlaybackService playbackService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback stop input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        bool success = await playbackService.StopAsync(
            sessionId,
            input.PlaybackSessionId,
            cancellationToken
        );

        return new PlaybackStopPayload(success);
    }

    /// <summary>
    /// Notifies the server of a seek operation and returns the nearest keyframe position.
    /// Used to optimize transcoding/remuxing by seeking to keyframe boundaries.
    /// </summary>
    /// <param name="input">Seek notification payload.</param>
    /// <param name="playbackService">Playback orchestration service.</param>
    /// <param name="sessionService">Session validation service.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The seek payload with the nearest keyframe position.</returns>
    [Authorize]
    public static async Task<PlaybackSeekPayload> PlaybackSeekAsync(
        PlaybackSeekInput input,
        [Service] IPlaybackService playbackService,
        [Service] ISessionService sessionService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePlaybackGraphQLInputError("Playback seek input is required.");
        }

        int sessionId = await ResolveSessionIdAsync(
            httpContextAccessor,
            sessionService,
            cancellationToken
        );

        var response = await playbackService.SeekAsync(
            sessionId,
            new PlaybackSeekRequest
            {
                PlaybackSessionId = input.PlaybackSessionId,
                TargetMs = input.TargetMs,
                MediaPartId = input.MediaPartId,
            },
            cancellationToken
        );

        return new PlaybackSeekPayload(
            response.KeyframeMs,
            response.GopDurationMs,
            response.HasGopIndex,
            response.OriginalTargetMs
        );
    }

    private static GraphQLException CreatePlaybackGraphQLInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder.New().SetMessage(message).SetCode("PLAYBACK_INPUT_INVALID").Build()
        );
    }

    private static GraphQLException CreatePlaybackSessionError()
    {
        return new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage("An active session is required for playback operations.")
                .SetCode("PLAYBACK_SESSION_REQUIRED")
                .Build()
        );
    }

    private static async Task<int> ResolveSessionIdAsync(
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
            throw CreatePlaybackSessionError();
        }

        if (!Guid.TryParse(sessionClaim, out Guid publicSessionId))
        {
            throw CreatePlaybackSessionError();
        }

        var session = await sessionService.ValidateSessionAsync(publicSessionId, cancellationToken);
        if (session is null)
        {
            throw CreatePlaybackSessionError();
        }

        return session.Id;
    }

    private static async Task<CoreMetadataItem> ResolveMetadataEntityAsync(
        Guid itemId,
        IMetadataService metadataService
    )
    {
        var metadata = await metadataService.GetByUuidAsync(itemId);
        if (metadata is null)
        {
            throw new GraphQLException(
                ErrorBuilder
                    .New()
                    .SetMessage($"Metadata item with ID '{itemId}' not found.")
                    .SetCode("METADATA_ITEM_NOT_FOUND")
                    .Build()
            );
        }

        return metadata;
    }
}
