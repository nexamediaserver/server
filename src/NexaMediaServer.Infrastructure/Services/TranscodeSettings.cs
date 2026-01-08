// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Represents transcode settings loaded from the database.
/// </summary>
public class TranscodeSettings
{
    /// <summary>
    /// Gets or sets the default video codec for DASH transcoding.
    /// </summary>
    public string DashVideoCodec { get; set; } = "h264";

    /// <summary>
    /// Gets or sets the default audio codec for DASH transcoding.
    /// </summary>
    public string DashAudioCodec { get; set; } = "aac";

    /// <summary>
    /// Gets or sets the DASH segment duration in seconds.
    /// </summary>
    public int DashSegmentDurationSeconds { get; set; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping is enabled.
    /// </summary>
    public bool EnableToneMapping { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred hardware acceleration (null = auto-detect).
    /// </summary>
    public HardwareAccelerationKind? UserPreferredAcceleration { get; set; }
}
