// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload returned when resuming an existing playback session.
/// </summary>
public sealed class PlaybackResumePayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackResumePayload"/> class.
    /// </summary>
    /// <param name="session">Playback session entity.</param>
    /// <param name="currentItemId">Current metadata item identifier.</param>
    /// <param name="playlistGeneratorId">Playlist generator identifier.</param>
    /// <param name="capabilityProfileVersion">Latest capability profile version.</param>
    /// <param name="capabilityVersionMismatch">Indicates whether the client's capability version is stale.</param>
    /// <param name="streamPlanJson">Serialized stream plan used to resume playback.</param>
    /// <param name="playbackUrl">URL to load for playback.</param>
    /// <param name="trickplayUrl">Optional trickplay track URL.</param>
    /// <param name="durationMs">Media duration in milliseconds.</param>
    /// <param name="playheadMs">Last known playhead position.</param>
    /// <param name="state">Last known playback state.</param>
    public PlaybackResumePayload(
        PlaybackSession session,
        Guid currentItemId,
        Guid playlistGeneratorId,
        int capabilityProfileVersion,
        bool capabilityVersionMismatch,
        string streamPlanJson,
        string playbackUrl,
        string? trickplayUrl,
        long? durationMs,
        long playheadMs,
        string state
    )
    {
        this.PlaybackSessionId = session.PlaybackSessionId;
        this.CurrentItemId = currentItemId;
        this.PlaylistGeneratorId = playlistGeneratorId;
        this.State = state;
        this.CapabilityProfileVersion = capabilityProfileVersion;
        this.CapabilityVersionMismatch = capabilityVersionMismatch;
        this.StreamPlanJson = streamPlanJson;
        this.PlaybackUrl = playbackUrl;
        this.TrickplayUrl = trickplayUrl;
        this.DurationMs = durationMs;
        this.PlayheadMs = playheadMs;
    }

    /// <summary>
    /// Gets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; }

    /// <summary>
    /// Gets the current metadata item identifier.
    /// </summary>
    [ID("Item")]
    public Guid CurrentItemId { get; }

    /// <summary>
    /// Gets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; }

    /// <summary>
    /// Gets the current playhead in milliseconds.
    /// </summary>
    public long PlayheadMs { get; }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the latest capability profile version known to the server.
    /// </summary>
    public int CapabilityProfileVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the client should refresh capabilities.
    /// </summary>
    public bool CapabilityVersionMismatch { get; }

    /// <summary>
    /// Gets the serialized stream plan for the current playback.
    /// </summary>
    public string StreamPlanJson { get; }

    /// <summary>
    /// Gets the playback URL the client should load when resuming.
    /// </summary>
    public string PlaybackUrl { get; }

    /// <summary>
    /// Gets the trickplay track URL when available.
    /// </summary>
    public string? TrickplayUrl { get; }

    /// <summary>
    /// Gets the duration of the media item in milliseconds.
    /// </summary>
    public long? DurationMs { get; }
}
