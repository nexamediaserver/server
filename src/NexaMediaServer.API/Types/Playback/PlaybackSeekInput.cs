// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload for notifying the server of a seek operation during playback.
/// Used to obtain the nearest keyframe position for optimal transcoding/remuxing.
/// </summary>
public sealed class PlaybackSeekInput
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the target seek position in milliseconds.
    /// </summary>
    public long TargetMs { get; set; }

    /// <summary>
    /// Gets or sets the current media part identifier.
    /// </summary>
    public int MediaPartId { get; set; }
}
