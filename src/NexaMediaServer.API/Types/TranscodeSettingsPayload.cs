// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after updating or retrieving transcode settings.
/// </summary>
/// <param name="EffectiveAcceleration">The currently active hardware acceleration.</param>
/// <param name="RecommendedAcceleration">The system-recommended hardware acceleration for this platform.</param>
/// <param name="UserPreferredAcceleration">The user's manually specified preference (null = auto).</param>
/// <param name="DetectedCapabilities">Dictionary of detected hardware acceleration capabilities.</param>
/// <param name="AvailableEncoders">List of available FFmpeg encoders.</param>
/// <param name="AvailableFilters">List of available FFmpeg filters.</param>
/// <param name="EnableToneMapping">Whether tone mapping is enabled.</param>
/// <param name="DashVideoCodec">Default video codec for DASH transcoding.</param>
/// <param name="DashAudioCodec">Default audio codec for DASH transcoding.</param>
/// <param name="DashSegmentDurationSeconds">DASH segment duration in seconds.</param>
public sealed record TranscodeSettingsPayload(
    HardwareAccelerationKind EffectiveAcceleration,
    HardwareAccelerationKind RecommendedAcceleration,
    HardwareAccelerationKind? UserPreferredAcceleration,
    Dictionary<string, bool> DetectedCapabilities,
    List<string> AvailableEncoders,
    List<string> AvailableFilters,
    bool EnableToneMapping,
    string DashVideoCodec,
    string DashAudioCodec,
    int DashSegmentDurationSeconds);
