// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response payload for a seek notification.
/// Contains the nearest keyframe position for optimal seeking during transcoding/remuxing.
/// </summary>
public sealed class PlaybackSeekResponse
{
    /// <summary>
    /// Gets or sets the nearest keyframe position in milliseconds.
    /// This is the position the transcoder/remuxer will seek to for faster feedback.
    /// </summary>
    public long KeyframeMs { get; set; }

    /// <summary>
    /// Gets or sets the duration of the keyframe's group of pictures in milliseconds.
    /// </summary>
    public long GopDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a GoP index was available for this seek.
    /// When false, the server falls back to the original target position.
    /// </summary>
    public bool HasGopIndex { get; set; }

    /// <summary>
    /// Gets or sets the original requested seek position in milliseconds.
    /// </summary>
    public long OriginalTargetMs { get; set; }
}
