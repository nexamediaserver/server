// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Parameters describing an HLS transcode request.
/// </summary>
public sealed class HlsTranscodeJob
{
    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the master playlist output path.
    /// </summary>
    public string MasterPlaylistPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the directory containing playlists and segments.
    /// </summary>
    public string OutputDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the variant identifier for this transcode (e.g., "720p").
    /// </summary>
    public string VariantId { get; init; } = "auto";

    /// <summary>
    /// Gets the desired video codec (ignored when copying video).
    /// </summary>
    public string VideoCodec { get; init; } = "h264";

    /// <summary>
    /// Gets the desired audio codec (ignored when copying audio).
    /// </summary>
    public string AudioCodec { get; init; } = "aac";

    /// <summary>
    /// Gets the target segment duration in seconds.
    /// </summary>
    public int SegmentSeconds { get; init; } = 6;

    /// <summary>
    /// Gets a value indicating whether the video stream should be copied.
    /// </summary>
    public bool CopyVideo { get; init; }

    /// <summary>
    /// Gets a value indicating whether the audio stream should be copied.
    /// </summary>
    public bool CopyAudio { get; init; }

    /// <summary>
    /// Gets a value indicating whether hardware acceleration should be used when available.
    /// </summary>
    public bool UseHardwareAcceleration { get; init; }

    /// <summary>
    /// Gets the hardware acceleration mode.
    /// </summary>
    public HardwareAccelerationKind HardwareAcceleration { get; init; }

    /// <summary>
    /// Gets a value indicating whether tone mapping should be applied.
    /// </summary>
    public bool EnableToneMapping { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is HDR content.
    /// </summary>
    public bool IsHdr { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is interlaced.
    /// </summary>
    public bool IsInterlaced { get; init; }

    /// <summary>
    /// Gets the rotation angle in degrees (0, 90, 180, 270).
    /// </summary>
    public int Rotation { get; init; }

    /// <summary>
    /// Gets the source video codec name (e.g., "h264", "hevc").
    /// </summary>
    public string SourceVideoCodec { get; init; } = "h264";

    /// <summary>
    /// Gets a value indicating whether hardware decoding should be used.
    /// </summary>
    public bool UseHardwareDecoder { get; init; }

    /// <summary>
    /// Gets the source video width.
    /// </summary>
    public int SourceWidth { get; init; }

    /// <summary>
    /// Gets the source video height.
    /// </summary>
    public int SourceHeight { get; init; }

    /// <summary>
    /// Gets the optional force_key_frames expression to align segments to GoP data.
    /// </summary>
    public string? ForceKeyFramesExpression { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source contains a video stream.
    /// </summary>
    public bool HasVideo { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the source contains an audio stream.
    /// </summary>
    public bool HasAudio { get; init; } = true;

    /// <summary>
    /// Gets the target video width for scaling.
    /// </summary>
    public int? TargetWidth { get; init; }

    /// <summary>
    /// Gets the target video height for scaling.
    /// </summary>
    public int? TargetHeight { get; init; }

    /// <summary>
    /// Gets the target video bitrate in bits per second.
    /// </summary>
    public int? VideoBitrate { get; init; }

    /// <summary>
    /// Gets the target audio bitrate in bits per second.
    /// </summary>
    public int? AudioBitrate { get; init; }

    /// <summary>
    /// Gets the target audio channel count for downmixing.
    /// </summary>
    public int? AudioChannels { get; init; }

    /// <summary>
    /// Gets the selected audio stream index.
    /// </summary>
    public int? AudioStreamIndex { get; init; }

    /// <summary>
    /// Gets the selected subtitle stream index for burn-in.
    /// </summary>
    public int? SubtitleStreamIndex { get; init; }

    /// <summary>
    /// Gets a value indicating whether to use fragmented MP4 (fMP4) instead of MPEG-TS.
    /// </summary>
    public bool UseFragmentedMp4 { get; init; } = true;
}
