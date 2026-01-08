// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Context information passed to video filters during pipeline execution.
/// </summary>
public sealed record VideoFilterContext
{
    /// <summary>
    /// Gets the hardware acceleration type being used.
    /// </summary>
    public required HardwareAccelerationKind HardwareAcceleration { get; init; }

    /// <summary>
    /// Gets the FFmpeg capabilities available on this system.
    /// </summary>
    public required IFfmpegCapabilities Capabilities { get; init; }

    /// <summary>
    /// Gets the source video codec.
    /// </summary>
    public required string SourceVideoCodec { get; init; }

    /// <summary>
    /// Gets the target video codec.
    /// </summary>
    public required string TargetVideoCodec { get; init; }

    /// <summary>
    /// Gets the source video width.
    /// </summary>
    public required int SourceWidth { get; init; }

    /// <summary>
    /// Gets the source video height.
    /// </summary>
    public required int SourceHeight { get; init; }

    /// <summary>
    /// Gets the target video width (null = no scaling).
    /// </summary>
    public int? TargetWidth { get; init; }

    /// <summary>
    /// Gets the target video height (null = no scaling).
    /// </summary>
    public int? TargetHeight { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is interlaced.
    /// </summary>
    public bool IsInterlaced { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source is HDR content.
    /// </summary>
    public bool IsHdr { get; init; }

    /// <summary>
    /// Gets a value indicating whether tone mapping should be applied.
    /// </summary>
    public bool EnableToneMapping { get; init; }

    /// <summary>
    /// Gets the rotation angle in degrees (0, 90, 180, 270).
    /// </summary>
    public int Rotation { get; init; }

    /// <summary>
    /// Gets a value indicating whether subtitles need to be burned in.
    /// </summary>
    public bool RequiresSubtitleOverlay { get; init; }

    /// <summary>
    /// Gets the subtitle file path for overlay (if required).
    /// </summary>
    public string? SubtitlePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the decoder outputs hardware frames.
    /// </summary>
    public bool IsHardwareDecoder { get; init; }

    /// <summary>
    /// Gets a value indicating whether the encoder accepts hardware frames.
    /// </summary>
    public bool IsHardwareEncoder { get; init; }
}
