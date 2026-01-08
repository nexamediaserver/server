// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// Media properties that can be evaluated in profile conditions.
/// </summary>
public static class ProfileConditionProperty
{
    /// <summary>
    /// The number of audio channels.
    /// </summary>
    public const string AudioChannels = "AudioChannels";

    /// <summary>
    /// The audio codec.
    /// </summary>
    public const string AudioCodec = "AudioCodec";

    /// <summary>
    /// The audio profile (e.g., LC, HE-AACv2).
    /// </summary>
    public const string AudioProfile = "AudioProfile";

    /// <summary>
    /// The audio sample rate in Hz.
    /// </summary>
    public const string AudioSampleRate = "AudioSampleRate";

    /// <summary>
    /// The audio bit depth.
    /// </summary>
    public const string AudioBitDepth = "AudioBitDepth";

    /// <summary>
    /// The audio bitrate in bits per second.
    /// </summary>
    public const string AudioBitrate = "AudioBitrate";

    /// <summary>
    /// The video codec.
    /// </summary>
    public const string VideoCodec = "VideoCodec";

    /// <summary>
    /// The video codec profile (e.g., High, Main, Main10).
    /// </summary>
    public const string VideoProfile = "VideoProfile";

    /// <summary>
    /// The video codec level (e.g., 4.1, 5.1).
    /// </summary>
    public const string VideoLevel = "VideoLevel";

    /// <summary>
    /// The video bit depth.
    /// </summary>
    public const string VideoBitDepth = "VideoBitDepth";

    /// <summary>
    /// The video width in pixels.
    /// </summary>
    public const string Width = "Width";

    /// <summary>
    /// The video height in pixels.
    /// </summary>
    public const string Height = "Height";

    /// <summary>
    /// The video bitrate in bits per second.
    /// </summary>
    public const string VideoBitrate = "VideoBitrate";

    /// <summary>
    /// The video frame rate.
    /// </summary>
    public const string VideoFramerate = "VideoFramerate";

    /// <summary>
    /// The number of reference frames.
    /// </summary>
    public const string RefFrames = "RefFrames";

    /// <summary>
    /// The video range type (SDR, HDR10, etc).
    /// </summary>
    public const string VideoRangeType = "VideoRangeType";

    /// <summary>
    /// Whether the video is interlaced.
    /// </summary>
    public const string IsInterlaced = "IsInterlaced";

    /// <summary>
    /// Whether the video is anamorphic.
    /// </summary>
    public const string IsAnamorphic = "IsAnamorphic";

    /// <summary>
    /// Whether the stream is secondary (not primary).
    /// </summary>
    public const string IsSecondaryAudio = "IsSecondaryAudio";

    /// <summary>
    /// The container format.
    /// </summary>
    public const string Container = "Container";

    /// <summary>
    /// Total bitrate of the file.
    /// </summary>
    public const string Bitrate = "Bitrate";

    /// <summary>
    /// Number of video streams.
    /// </summary>
    public const string NumVideoStreams = "NumVideoStreams";

    /// <summary>
    /// Number of audio streams.
    /// </summary>
    public const string NumAudioStreams = "NumAudioStreams";
}
