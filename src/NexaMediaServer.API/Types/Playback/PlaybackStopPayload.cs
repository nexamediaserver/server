// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after stopping a playback session.
/// </summary>
public sealed class PlaybackStopPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStopPayload"/> class.
    /// </summary>
    /// <param name="success">Whether a playback session was removed.</param>
    public PlaybackStopPayload(bool success)
    {
        this.Success = success;
    }

    /// <summary>
    /// Gets a value indicating whether the playback session was stopped.
    /// </summary>
    public bool Success { get; }
}
