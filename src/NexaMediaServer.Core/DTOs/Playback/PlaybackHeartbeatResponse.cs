// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response from a playback heartbeat operation.
/// </summary>
public sealed class PlaybackHeartbeatResponse
{
    /// <summary>
    /// Gets or sets the playback session identifier that was updated.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the current capability profile version for the session.
    /// </summary>
    public int CapabilityProfileVersion { get; set; }
}
