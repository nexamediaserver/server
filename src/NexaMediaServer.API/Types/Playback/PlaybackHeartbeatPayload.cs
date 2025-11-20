// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload returned after recording a playback heartbeat.
/// </summary>
public sealed class PlaybackHeartbeatPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackHeartbeatPayload"/> class.
    /// </summary>
    /// <param name="playbackSessionId">Playback session identifier.</param>
    /// <param name="capabilityProfileVersion">Latest capability profile version.</param>
    /// <param name="capabilityVersionMismatch">Indicates whether the client's capability version is stale.</param>
    public PlaybackHeartbeatPayload(
        Guid playbackSessionId,
        int capabilityProfileVersion,
        bool capabilityVersionMismatch
    )
    {
        this.PlaybackSessionId = playbackSessionId;
        this.CapabilityProfileVersion = capabilityProfileVersion;
        this.CapabilityVersionMismatch = capabilityVersionMismatch;
    }

    /// <summary>
    /// Gets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; }

    /// <summary>
    /// Gets the latest capability profile version known to the server.
    /// </summary>
    public int CapabilityProfileVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the client should refresh capabilities.
    /// </summary>
    public bool CapabilityVersionMismatch { get; }
}
