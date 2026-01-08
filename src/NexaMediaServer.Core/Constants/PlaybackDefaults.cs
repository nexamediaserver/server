// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Shared defaults for playback sessions and playlist generation.
/// </summary>
public static class PlaybackDefaults
{
    /// <summary>
    /// Number of items to materialize per generator chunk.
    /// </summary>
    public const int PlaylistChunkSize = 100;

    /// <summary>
    /// Minimum number of lookahead items returned with a decision.
    /// </summary>
    public const int MinimumLookahead = 1;

    /// <summary>
    /// Heartbeat interval in seconds used to refresh playback sessions.
    /// Clients should send heartbeats at this interval to keep sessions alive.
    /// </summary>
    public const int HeartbeatIntervalSeconds = 15;

    /// <summary>
    /// Default inactivity expiry for playback sessions and generators in days.
    /// </summary>
    public const int ExpiryDays = 30;
}
