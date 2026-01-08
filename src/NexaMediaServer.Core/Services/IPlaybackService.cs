// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Defines playback orchestration operations (capability negotiation, start, heartbeat, decisions).
/// </summary>
public interface IPlaybackService
{
    /// <summary>
    /// Creates or updates a capability profile for a session.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="input">The capability payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted capability profile.</returns>
    Task<CapabilityProfile> UpsertCapabilityProfileAsync(
        int sessionId,
        CapabilityProfileInput input,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Starts playback for the specified item and returns stream planning details.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="request">The playback start request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playback session identifiers and initial plan.</returns>
    Task<PlaybackStartResponse> StartPlaybackAsync(
        int sessionId,
        PlaybackStartRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Processes a heartbeat update, refreshing expiry and storing progress.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="request">The heartbeat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The heartbeat response with current capability version.</returns>
    Task<PlaybackHeartbeatResponse> HeartbeatAsync(
        int sessionId,
        PlaybackHeartbeatRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Computes the next action for a playback session after an item completes or needs a decision.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="request">The decision request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decision response.</returns>
    Task<PlaybackDecisionResponse> DecideAsync(
        int sessionId,
        PlaybackDecisionRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Attempts to resume a playback session by identifier.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="playbackSessionId">The public playback session id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resume response with session details, or null if not found.</returns>
    Task<PlaybackResumeResponse?> ResumeAsync(
        int sessionId,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Processes a seek notification and returns the nearest keyframe position for transcoding/remuxing.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="request">The seek request with target position and media part.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The seek response with the nearest keyframe position.</returns>
    Task<PlaybackSeekResponse> SeekAsync(
        int sessionId,
        PlaybackSeekRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Stops an active playback session, removing persisted state and cached assets.
    /// </summary>
    /// <param name="sessionId">The owning session identifier.</param>
    /// <param name="playbackSessionId">The playback session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when a session was removed; otherwise <c>false</c>.</returns>
    Task<bool> StopAsync(int sessionId, Guid playbackSessionId, CancellationToken cancellationToken);
}
