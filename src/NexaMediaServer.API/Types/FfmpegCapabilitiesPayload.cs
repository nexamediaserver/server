// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload containing detected FFmpeg capabilities and recommendations.
/// </summary>
/// <param name="Version">The detected FFmpeg version string.</param>
/// <param name="SupportedHardwareAccelerators">List of supported hardware acceleration types.</param>
/// <param name="SupportedEncoders">List of all supported encoders.</param>
/// <param name="SupportedFilters">List of all supported filters.</param>
/// <param name="RecommendedAcceleration">The recommended hardware acceleration for this platform.</param>
/// <param name="IsDetected">Whether capability detection has completed.</param>
public sealed record FfmpegCapabilitiesPayload(
    string Version,
    List<HardwareAccelerationKind> SupportedHardwareAccelerators,
    List<string> SupportedEncoders,
    List<string> SupportedFilters,
    HardwareAccelerationKind RecommendedAcceleration,
    bool IsDetected);
