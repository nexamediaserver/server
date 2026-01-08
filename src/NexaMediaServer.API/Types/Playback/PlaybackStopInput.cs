// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used to stop an active playback session.
/// </summary>
public sealed class PlaybackStopInput
{
    /// <summary>
    /// Gets or sets the playback session identifier to stop.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }
}
