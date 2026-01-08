// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Transcode settings mutations.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates transcode settings (admin only).
    /// </summary>
    /// <param name="input">The transcode settings to update.</param>
    /// <param name="transcodeOptions">The transcode options monitor.</param>
    /// <param name="capabilities">The FFmpeg capabilities.</param>
    /// <returns>The updated transcode settings.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<TranscodeSettingsPayload> UpdateTranscodeSettingsAsync(
        UpdateTranscodeSettingsInput input,
        [Service] IOptionsMonitor<TranscodeOptions> transcodeOptions,
        [Service] IFfmpegCapabilities capabilities)
    {
        if (input is null)
        {
            throw CreateGraphQLInputError("Transcode settings input is required.");
        }

        var options = transcodeOptions.CurrentValue;

        // Update settings
        if (input.UserPreferredAcceleration.HasValue)
        {
            // Validate the selected acceleration is available
            if (input.UserPreferredAcceleration.Value != HardwareAccelerationKind.None &&
                !capabilities.SupportsHwAccel(input.UserPreferredAcceleration.Value))
            {
                throw CreateGraphQLInputError(
                    $"Hardware acceleration '{input.UserPreferredAcceleration.Value}' is not supported on this system. " +
                    $"Detected capabilities: {string.Join(", ", capabilities.SupportedHwAccel)}");
            }

            options.UserPreferredAcceleration = input.UserPreferredAcceleration;
        }

        if (input.EnableToneMapping.HasValue)
        {
            options.EnableToneMapping = input.EnableToneMapping.Value;
        }

        if (!string.IsNullOrWhiteSpace(input.DashVideoCodec))
        {
            options.DashVideoCodec = input.DashVideoCodec;
        }

        if (!string.IsNullOrWhiteSpace(input.DashAudioCodec))
        {
            options.DashAudioCodec = input.DashAudioCodec;
        }

        if (input.DashSegmentDurationSeconds.HasValue)
        {
            if (input.DashSegmentDurationSeconds.Value < 1 || input.DashSegmentDurationSeconds.Value > 30)
            {
                throw CreateGraphQLInputError("DASH segment duration must be between 1 and 30 seconds.");
            }

            options.DashSegmentDurationSeconds = input.DashSegmentDurationSeconds.Value;
        }

        // Database persistence pending ServerSetting infrastructure (tracked in backlog)
        await Task.CompletedTask; // Satisfy async signature

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
}
