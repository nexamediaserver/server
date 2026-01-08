// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Indicates why transcoding is required instead of direct play or direct stream.
/// </summary>
[Flags]
public enum TranscodeReason
{
    /// <summary>
    /// No transcoding required; direct play is possible.
    /// </summary>
    None = 0,

    /// <summary>
    /// Container format not supported by client.
    /// </summary>
    ContainerNotSupported = 1 << 0,

    /// <summary>
    /// Video codec not supported by client.
    /// </summary>
    VideoCodecNotSupported = 1 << 1,

    /// <summary>
    /// Audio codec not supported by client.
    /// </summary>
    AudioCodecNotSupported = 1 << 2,

    /// <summary>
    /// Subtitle codec requires burn-in (not externally deliverable).
    /// </summary>
    SubtitleCodecNotSupported = 1 << 3,

    /// <summary>
    /// Video bitrate exceeds client maximum.
    /// </summary>
    VideoBitrateTooHigh = 1 << 4,

    /// <summary>
    /// Audio bitrate exceeds client maximum.
    /// </summary>
    AudioBitrateTooHigh = 1 << 5,

    /// <summary>
    /// Video resolution exceeds client maximum.
    /// </summary>
    VideoResolutionTooHigh = 1 << 6,

    /// <summary>
    /// Video level exceeds client capability.
    /// </summary>
    VideoLevelNotSupported = 1 << 7,

    /// <summary>
    /// Video profile not supported by client.
    /// </summary>
    VideoProfileNotSupported = 1 << 8,

    /// <summary>
    /// Video reference frames exceed client capability.
    /// </summary>
    VideoRefFramesNotSupported = 1 << 9,

    /// <summary>
    /// Video bit depth not supported by client.
    /// </summary>
    VideoBitDepthNotSupported = 1 << 10,

    /// <summary>
    /// Audio channel count exceeds client capability.
    /// </summary>
    AudioChannelsNotSupported = 1 << 11,

    /// <summary>
    /// Audio sample rate not supported by client.
    /// </summary>
    AudioSampleRateNotSupported = 1 << 12,

    /// <summary>
    /// HDR content requires tone mapping for SDR client.
    /// </summary>
    HdrNotSupported = 1 << 13,

    /// <summary>
    /// Anamorphic video not supported by client.
    /// </summary>
    AnamorphicNotSupported = 1 << 14,

    /// <summary>
    /// Interlaced video not supported by client.
    /// </summary>
    InterlacedNotSupported = 1 << 15,

    /// <summary>
    /// User explicitly requested transcoding.
    /// </summary>
    UserRequested = 1 << 16,

    /// <summary>
    /// Server configuration forces transcoding.
    /// </summary>
    ServerConfiguration = 1 << 17,

    /// <summary>
    /// Video framerate exceeds client capability.
    /// </summary>
    VideoFramerateNotSupported = 1 << 18,

    /// <summary>
    /// Secondary audio track requires re-encoding.
    /// </summary>
    SecondaryAudioTrack = 1 << 19,

    /// <summary>
    /// Subtitle burn-in requested.
    /// </summary>
    SubtitleBurnIn = 1 << 20,

    /// <summary>
    /// Audio profile not supported by client.
    /// </summary>
    AudioProfileNotSupported = 1 << 21,

    /// <summary>
    /// Audio bit depth not supported by client.
    /// </summary>
    AudioBitDepthNotSupported = 1 << 22,

    /// <summary>
    /// Audio bitrate not supported by client.
    /// </summary>
    AudioBitrateNotSupported = 1 << 23,

    /// <summary>
    /// Video bitrate not supported by client.
    /// </summary>
    VideoBitrateNotSupported = 1 << 24,

    /// <summary>
    /// Video resolution not supported by client.
    /// </summary>
    VideoResolutionNotSupported = 1 << 25,

    /// <summary>
    /// Reference frames count not supported by client.
    /// </summary>
    RefFramesNotSupported = 1 << 26,

    /// <summary>
    /// Video range type (SDR/HDR) not supported by client.
    /// </summary>
    VideoRangeTypeNotSupported = 1 << 27,

    /// <summary>
    /// Interlaced video not supported by client.
    /// </summary>
    InterlacedVideoNotSupported = 1 << 28,

    /// <summary>
    /// Unknown reason for transcoding.
    /// </summary>
    Unknown = 1 << 29,
}
