// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Transcode settings queries.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Gets the current transcode settings and detected hardware capabilities (admin only).
    /// </summary>
    /// <param name="transcodeOptions">The transcode options.</param>
    /// <param name="capabilities">The FFmpeg capabilities.</param>
    /// <returns>The current transcode settings.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static TranscodeSettingsPayload GetTranscodeSettings(
        [Service] IOptionsMonitor<TranscodeOptions> transcodeOptions,
        [Service] IFfmpegCapabilities capabilities)
    {
        var options = transcodeOptions.CurrentValue;

        return new TranscodeSettingsPayload(
            EffectiveAcceleration: options.EffectiveAcceleration,
            RecommendedAcceleration: options.RecommendedAcceleration,
            UserPreferredAcceleration: options.UserPreferredAcceleration,
            DetectedCapabilities: options.DetectedCapabilities,
            AvailableEncoders: options.AvailableEncoders,
            AvailableFilters: options.AvailableFilters,
            EnableToneMapping: options.EnableToneMapping,
            DashVideoCodec: options.DashVideoCodec,
            DashAudioCodec: options.DashAudioCodec,
            DashSegmentDurationSeconds: options.DashSegmentDurationSeconds);
    }

    /// <summary>
    /// Gets the detected FFmpeg capabilities and hardware acceleration recommendations (admin only).
    /// </summary>
    /// <param name="capabilities">The FFmpeg capabilities service.</param>
    /// <returns>The detected FFmpeg capabilities.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static FfmpegCapabilitiesPayload GetFfmpegCapabilities(
        [Service] IFfmpegCapabilities capabilities)
    {
        return new FfmpegCapabilitiesPayload(
            Version: capabilities.Version,
            IsDetected: capabilities.IsDetected,
            SupportedHardwareAccelerators: capabilities.SupportedHwAccel.ToList(),
            SupportedEncoders: capabilities.SupportedEncoders.ToList(),
            SupportedFilters: capabilities.SupportedFilters.ToList(),
            RecommendedAcceleration: capabilities.RecommendedAcceleration);
    }
}
