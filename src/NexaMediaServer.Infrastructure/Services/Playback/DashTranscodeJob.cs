// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Parameters describing a DASH transcode request.
/// </summary>
public sealed class DashTranscodeJob
{
    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the manifest output path.
    /// </summary>
    public string ManifestPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the directory containing manifest and segments.
    /// </summary>
    public string OutputDirectory { get; init; } = string.Empty;

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
    public int SegmentSeconds { get; init; } = 4;

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
    /// Gets the optional force_key_frames expression to align segments to GoP data.
    /// </summary>
    public string? ForceKeyFramesExpression { get; init; }
}