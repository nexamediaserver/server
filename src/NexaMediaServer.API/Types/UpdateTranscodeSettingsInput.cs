// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for updating transcode settings.
/// </summary>
public sealed record UpdateTranscodeSettingsInput
{
    /// <summary>
    /// Gets the user's preferred hardware acceleration (null = use auto-detected).
    /// </summary>
    public HardwareAccelerationKind? UserPreferredAcceleration { get; init; }

    /// <summary>
    /// Gets a value indicating whether tone mapping is enabled.
    /// </summary>
    public bool? EnableToneMapping { get; init; }

    /// <summary>
    /// Gets the default video codec for DASH transcoding.
    /// </summary>
    public string? DashVideoCodec { get; init; }

    /// <summary>
    /// Gets the default audio codec for DASH transcoding.
    /// </summary>
    public string? DashAudioCodec { get; init; }

    /// <summary>
    /// Gets the DASH segment duration in seconds.
    /// </summary>
    public int? DashSegmentDurationSeconds { get; init; }
}
