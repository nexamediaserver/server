// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using HotChocolate.Authorization;
using HotChocolate.Resolvers;

using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Logging;
using NexaMediaServer.Infrastructure.Services;

using Serilog.Events;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL mutation operations for server settings.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates server-wide configuration settings. Only specified fields are updated.
    /// </summary>
    /// <param name="input">The settings to update.</param>
    /// <param name="context">The resolver context.</param>
    /// <param name="settingService">The server setting service.</param>
    /// <param name="transcodeOptions">Transcode options for validation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated server settings.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<ServerSettingsPayload> UpdateServerSettingsAsync(
        UpdateServerSettingsInput input,
        IResolverContext context,
        [Service] IServerSettingService settingService,
        [Service] IOptionsMonitor<TranscodeOptions> transcodeOptions,
        CancellationToken cancellationToken)
    {
        // Validate inputs before applying
        if (input.MaxStreamingBitrate.HasValue &&
            (input.MaxStreamingBitrate.Value < 1_000_000 ||
                input.MaxStreamingBitrate.Value > 1_000_000_000))
        {
            throw new ArgumentException(
                "MaxStreamingBitrate must be between 1 Mbps and 1000 Mbps",
                nameof(input));
        }

        if (input.DashSegmentDurationSeconds.HasValue &&
            (input.DashSegmentDurationSeconds.Value < 1 ||
                input.DashSegmentDurationSeconds.Value > 30))
        {
            throw new ArgumentException(
                "DashSegmentDurationSeconds must be between 1 and 30",
                nameof(input));
        }

        var availableEncoders = transcodeOptions.CurrentValue.AvailableEncoders;
        if (input.DashVideoCodec is not null)
        {
            if (string.IsNullOrWhiteSpace(input.DashVideoCodec))
            {
                throw new ArgumentException(
                    "DashVideoCodec cannot be empty",
                    nameof(input));
            }

            if (availableEncoders.Count > 0 &&
                !availableEncoders.Contains(input.DashVideoCodec, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"DashVideoCodec '{input.DashVideoCodec}' is not available in detected encoders",
                    nameof(input));
            }
        }

        if (input.DashAudioCodec is not null)
        {
            if (string.IsNullOrWhiteSpace(input.DashAudioCodec))
            {
                throw new ArgumentException(
                    "DashAudioCodec cannot be empty",
                    nameof(input));
            }

            if (availableEncoders.Count > 0 &&
                !availableEncoders.Contains(input.DashAudioCodec, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"DashAudioCodec '{input.DashAudioCodec}' is not available in detected encoders",
                    nameof(input));
            }
        }

        if (input.LogLevel is not null &&
            !Enum.TryParse<LogEventLevel>(input.LogLevel, ignoreCase: true, out _))
        {
            throw new ArgumentException(
                "LogLevel must be one of: Debug, Information, Warning, Error, Fatal",
                nameof(input));
        }

        // Update only the fields that were provided
        if (input.ServerName is not null)
        {
            await settingService.SetAsync(
                ServerSettingKeys.ServerName,
                input.ServerName,
                cancellationToken);
        }

        if (input.MaxStreamingBitrate.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.MaxStreamingBitrate,
                input.MaxStreamingBitrate.Value,
                cancellationToken);
        }

        if (input.PreferH265.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.PreferH265,
                input.PreferH265.Value,
                cancellationToken);
        }

        if (input.AllowRemuxing.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.AllowRemuxing,
                input.AllowRemuxing.Value,
                cancellationToken);
        }

        if (input.AllowHEVCEncoding.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.AllowHEVCEncoding,
                input.AllowHEVCEncoding.Value,
                cancellationToken);
        }

        if (input.DashVideoCodec is not null)
        {
            await settingService.SetAsync(
                ServerSettingKeys.DashVideoCodec,
                input.DashVideoCodec,
                cancellationToken);
        }

        if (input.DashAudioCodec is not null)
        {
            await settingService.SetAsync(
                ServerSettingKeys.DashAudioCodec,
                input.DashAudioCodec,
                cancellationToken);
        }

        if (input.DashSegmentDurationSeconds.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.DashSegmentDurationSeconds,
                input.DashSegmentDurationSeconds.Value,
                cancellationToken);
        }

        if (input.EnableToneMapping.HasValue)
        {
            await settingService.SetAsync(
                ServerSettingKeys.EnableToneMapping,
                input.EnableToneMapping.Value,
                cancellationToken);
        }

        if (input.UserPreferredAcceleration.HasValue)
        {
            await settingService.SetValueAsync(
                ServerSettingKeys.UserPreferredAcceleration,
                input.UserPreferredAcceleration.Value.ToString(),
                cancellationToken);
        }

        if (input.AllowedTags is not null)
        {
            var allowedTagsJson = JsonSerializer.Serialize(input.AllowedTags);
            await settingService.SetValueAsync(
                ServerSettingKeys.AllowedTags,
                allowedTagsJson,
                cancellationToken);
            // Reload tag moderation service
            var tagModerationService = context.Services.GetRequiredService<ITagModerationService>();
            await ((TagModerationService)tagModerationService).ReloadSettingsAsync(cancellationToken);
        }

        if (input.BlockedTags is not null)
        {
            var blockedTagsJson = JsonSerializer.Serialize(input.BlockedTags);
            await settingService.SetValueAsync(
                ServerSettingKeys.BlockedTags,
                blockedTagsJson,
                cancellationToken);
            // Reload tag moderation service
            var tagModerationService = context.Services.GetRequiredService<ITagModerationService>();
            await ((TagModerationService)tagModerationService).ReloadSettingsAsync(cancellationToken);
        }

        if (input.GenreMappings is not null)
        {
            var genreMappingsJson = JsonSerializer.Serialize(input.GenreMappings);
            await settingService.SetValueAsync(
                ServerSettingKeys.GenreMappings,
                genreMappingsJson,
                cancellationToken);
            // Reload genre normalization service
            var genreNormalizationService = context.Services.GetRequiredService<IGenreNormalizationService>();
            await ((GenreNormalizationService)genreNormalizationService).ReloadSettingsAsync(
                cancellationToken);
        }

        if (input.LogLevel is not null)
        {
            await settingService.SetAsync(
                ServerSettingKeys.LogLevel,
                input.LogLevel,
                cancellationToken);
            // Apply log level change immediately
            LoggingConfiguration.TrySetMinimumLevel(input.LogLevel);
        }

        // Reload transcode settings if any were changed
        if (input.DashVideoCodec is not null ||
            input.DashAudioCodec is not null ||
            input.DashSegmentDurationSeconds.HasValue ||
            input.EnableToneMapping.HasValue ||
            input.UserPreferredAcceleration.HasValue)
        {
            var transcodeSettingsService = context.Services.GetRequiredService<TranscodeSettingsService>();
            await transcodeSettingsService.ReloadSettingsAsync(cancellationToken);
        }

        // Return the current state after updates - reuse query logic
        return await Query.GetServerSettingsAsync(settingService, cancellationToken);
    }
}
