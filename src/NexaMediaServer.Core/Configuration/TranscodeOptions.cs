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
    /// Gets or sets the user's preferred hardware acceleration override (null = use auto-detected).
    /// </summary>
    public HardwareAccelerationKind? UserPreferredAcceleration { get; set; }

    /// <summary>
    /// Gets or sets the detected hardware acceleration capabilities.
    /// </summary>
    public Dictionary<string, bool> DetectedCapabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of available hardware encoders detected on startup.
    /// </summary>
    public List<string> AvailableEncoders { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of available hardware filters detected on startup.
    /// </summary>
    public List<string> AvailableFilters { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommended hardware acceleration for the current platform.
    /// </summary>
    public HardwareAccelerationKind RecommendedAcceleration { get; set; }

    /// <summary>
    /// Gets the effective hardware acceleration to use (user preference or recommended default).
    /// </summary>
    public HardwareAccelerationKind EffectiveAcceleration =>
        this.UserPreferredAcceleration ?? this.RecommendedAcceleration;

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

    /// <summary>
    /// Gets or sets the target segment duration for HLS manifests.
    /// </summary>
    public int HlsSegmentDurationSeconds { get; set; } = 6;

    /// <summary>
    /// Gets or sets the default video codec to use for HLS transcoding.
    /// </summary>
    public string HlsVideoCodec { get; set; } = "h264";

    /// <summary>
    /// Gets or sets the default audio codec to use for HLS transcoding.
    /// </summary>
    public string HlsAudioCodec { get; set; } = "aac";

    /// <summary>
    /// Gets or sets the maximum number of concurrent transcode jobs.
    /// </summary>
    public int MaxConcurrentTranscodes { get; set; } = 2;

    /// <summary>
    /// Gets or sets the idle timeout in seconds before a transcode job is killed.
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to prefer DASH over HLS when both are supported.
    /// </summary>
    public bool PreferDash { get; set; } = true;
}