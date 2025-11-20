// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Transcoding-related server settings.
/// </summary>
public sealed class TranscodeOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "Transcode";

    /// <summary>
    /// Gets or sets the preferred hardware acceleration mode (applies when compatible).
    /// </summary>
    public HardwareAccelerationKind HardwareAcceleration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping is allowed.
    /// </summary>
    public bool EnableToneMapping { get; set; }

    /// <summary>
    /// Gets or sets the target segment duration for DASH manifests.
    /// </summary>
    public int DashSegmentDurationSeconds { get; set; } = 4;

    /// <summary>
    /// Gets or sets the default video codec to use when transcoding.
    /// </summary>
    public string DashVideoCodec { get; set; } = "h264";

    /// <summary>
    /// Gets or sets the default audio codec to use when transcoding.
    /// </summary>
    public string DashAudioCodec { get; set; } = "aac";
}