// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after processing a seek notification.
/// Contains the nearest keyframe position for optimal seeking during transcoding/remuxing.
/// </summary>
public sealed class PlaybackSeekPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackSeekPayload"/> class.
    /// </summary>
    /// <param name="keyframeMs">The nearest keyframe position in milliseconds.</param>
    /// <param name="gopDurationMs">The GoP duration in milliseconds.</param>
    /// <param name="hasGopIndex">Whether a GoP index was available.</param>
    /// <param name="originalTargetMs">The original target position.</param>
    public PlaybackSeekPayload(
        long keyframeMs,
        long gopDurationMs,
        bool hasGopIndex,
        long originalTargetMs
    )
    {
        this.KeyframeMs = keyframeMs;
        this.GopDurationMs = gopDurationMs;
        this.HasGopIndex = hasGopIndex;
        this.OriginalTargetMs = originalTargetMs;
    }

    /// <summary>
    /// Gets the nearest keyframe position in milliseconds.
    /// This is the position the transcoder/remuxer will seek to for faster feedback.
    /// </summary>
    public long KeyframeMs { get; init; }

    /// <summary>
    /// Gets the duration of the keyframe's group of pictures in milliseconds.
    /// </summary>
    public long GopDurationMs { get; init; }

    /// <summary>
    /// Gets a value indicating whether a GoP index was available for this seek.
    /// When false, <see cref="KeyframeMs"/> equals the original target position.
    /// </summary>
    public bool HasGopIndex { get; init; }

    /// <summary>
    /// Gets the original requested seek position in milliseconds.
    /// </summary>
    public long OriginalTargetMs { get; init; }
}
