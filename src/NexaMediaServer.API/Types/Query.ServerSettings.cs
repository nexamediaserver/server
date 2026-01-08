// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using HotChocolate.Authorization;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL query operations for server settings.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Gets the current server-wide configuration settings.
    /// </summary>
    /// <param name="settingService">The server setting service.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current server settings.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<ServerSettingsPayload> GetServerSettingsAsync(
        [Service] IServerSettingService settingService,
        CancellationToken cancellationToken)
    {
        var serverName = await settingService.GetAsync(
            ServerSettingKeys.ServerName,
            ServerSettingDefaults.ServerName,
            cancellationToken);

        var maxStreamingBitrate = await settingService.GetAsync(
            ServerSettingKeys.MaxStreamingBitrate,
            ServerSettingDefaults.MaxStreamingBitrate,
            cancellationToken);

        var preferH265 = await settingService.GetAsync(
            ServerSettingKeys.PreferH265,
            ServerSettingDefaults.PreferH265,
            cancellationToken);

        var allowRemuxing = await settingService.GetAsync(
            ServerSettingKeys.AllowRemuxing,
            ServerSettingDefaults.AllowRemuxing,
            cancellationToken);

        var allowHEVCEncoding = await settingService.GetAsync(
            ServerSettingKeys.AllowHEVCEncoding,
            ServerSettingDefaults.AllowHEVCEncoding,
            cancellationToken);

        var dashVideoCodec = await settingService.GetAsync(
            ServerSettingKeys.DashVideoCodec,
            ServerSettingDefaults.DashVideoCodec,
            cancellationToken);

        var dashAudioCodec = await settingService.GetAsync(
            ServerSettingKeys.DashAudioCodec,
            ServerSettingDefaults.DashAudioCodec,
            cancellationToken);

        var dashSegmentDuration = await settingService.GetAsync(
            ServerSettingKeys.DashSegmentDurationSeconds,
            ServerSettingDefaults.DashSegmentDurationSeconds,
            cancellationToken);

        var enableToneMapping = await settingService.GetAsync(
            ServerSettingKeys.EnableToneMapping,
            ServerSettingDefaults.EnableToneMapping,
            cancellationToken);

        var userPreferredAccelStr = await settingService.GetValueAsync(
            ServerSettingKeys.UserPreferredAcceleration,
            cancellationToken);

        HardwareAccelerationKind? userPreferredAccel = null;
        if (!string.IsNullOrWhiteSpace(userPreferredAccelStr) &&
            Enum.TryParse<HardwareAccelerationKind>(userPreferredAccelStr, out var parsed))
        {
            userPreferredAccel = parsed;
        }

        var allowedTagsJson = await settingService.GetValueAsync(
            ServerSettingKeys.AllowedTags,
            cancellationToken);
        var allowedTags = !string.IsNullOrWhiteSpace(allowedTagsJson)
            ? JsonSerializer.Deserialize<List<string>>(allowedTagsJson) ?? []
            : [];

        var blockedTagsJson = await settingService.GetValueAsync(
            ServerSettingKeys.BlockedTags,
            cancellationToken);
        var blockedTags = !string.IsNullOrWhiteSpace(blockedTagsJson)
            ? JsonSerializer.Deserialize<List<string>>(blockedTagsJson) ?? []
            : [];

        var genreMappingsJson = await settingService.GetValueAsync(
            ServerSettingKeys.GenreMappings,
            cancellationToken);
        var genreMappings = !string.IsNullOrWhiteSpace(genreMappingsJson)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(genreMappingsJson)
                ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();

        var logLevel = await settingService.GetAsync(
            ServerSettingKeys.LogLevel,
            ServerSettingDefaults.LogLevel,
            cancellationToken);

        return new ServerSettingsPayload(
            serverName,
            maxStreamingBitrate,
            preferH265,
            allowRemuxing,
            allowHEVCEncoding,
            dashVideoCodec,
            dashAudioCodec,
            dashSegmentDuration,
            enableToneMapping,
            userPreferredAccel,
            allowedTags,
            blockedTags,
            genreMappings,
            logLevel);
    }
}
