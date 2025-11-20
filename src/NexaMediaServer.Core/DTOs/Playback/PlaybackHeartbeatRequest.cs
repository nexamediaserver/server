// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Request payload used to submit playback heartbeats.
/// </summary>
public sealed class PlaybackHeartbeatRequest
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the playhead position in milliseconds.
    /// </summary>
    public long PlayheadMs { get; set; }

    /// <summary>
    /// Gets or sets the playback state (playing, paused, buffering).
    /// </summary>
    public string State { get; set; } = "playing";

    /// <summary>
    /// Gets or sets the current media part identifier, if known.
    /// </summary>
    public int? MediaPartId { get; set; }
}
