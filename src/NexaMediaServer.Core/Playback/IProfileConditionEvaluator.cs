// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// Service for evaluating profile conditions against media properties.
/// </summary>
public interface IProfileConditionEvaluator
{
    /// <summary>
    /// Evaluates direct play conditions for the given media against the capability profile.
    /// </summary>
    /// <param name="properties">The media properties to evaluate.</param>
    /// <param name="capabilities">The client capability profile.</param>
    /// <param name="mediaType">The media type (Video, Audio).</param>
    /// <param name="forTranscoding">Whether evaluation is for a transcoding decision.</param>
    /// <returns>The evaluation result with pass/fail and reasons.</returns>
    ProfileEvaluationResult EvaluateConditions(
        MediaProperties properties,
        PlaybackCapabilities capabilities,
        string mediaType,
        bool forTranscoding = false);

    /// <summary>
    /// Checks if the container is supported for direct play.
    /// </summary>
    /// <param name="container">The container format.</param>
    /// <param name="capabilities">The client capability profile.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>True if the container matches any direct play profile.</returns>
    bool SupportsContainer(
        string? container,
        PlaybackCapabilities capabilities,
        string mediaType);

    /// <summary>
    /// Checks if the video codec is supported for direct play.
    /// </summary>
    /// <param name="codec">The video codec.</param>
    /// <param name="profile">The matching direct play profile.</param>
    /// <returns>True if the codec is supported.</returns>
    bool SupportsVideoCodec(string? codec, DirectPlayProfile profile);

    /// <summary>
    /// Checks if the audio codec is supported for direct play.
    /// </summary>
    /// <param name="codec">The audio codec.</param>
    /// <param name="profile">The matching direct play profile.</param>
    /// <returns>True if the codec is supported.</returns>
    bool SupportsAudioCodec(string? codec, DirectPlayProfile profile);

    /// <summary>
    /// Determines the best transcoding profile for the given media.
    /// </summary>
    /// <param name="properties">The media properties.</param>
    /// <param name="capabilities">The client capability profile.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>The best matching transcoding profile, or null if none match.</returns>
    TranscodingProfile? SelectTranscodingProfile(
        MediaProperties properties,
        PlaybackCapabilities capabilities,
        string mediaType);
}
