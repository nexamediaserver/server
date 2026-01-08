// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Request payload when the client asks for a decision on what to do next.
/// </summary>
public sealed class PlaybackDecisionRequest
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the current metadata item identifier.
    /// </summary>
    public int CurrentMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the current playback status (ended, playing, paused).
    /// </summary>
    public string Status { get; set; } = "ended";

    /// <summary>
    /// Gets or sets the current progress in milliseconds reported by the client.
    /// </summary>
    public long ProgressMs { get; set; }

    /// <summary>
    /// Gets or sets the target playlist index when status is "jump".
    /// </summary>
    public int? JumpIndex { get; set; }
}
