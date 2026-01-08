// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service for managing transcode settings from the database with in-memory caching.
/// </summary>
public class TranscodeSettingsService : IDisposable
{
    private readonly IServerSettingService serverSettingService;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private TranscodeSettings cachedSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeSettingsService"/> class.
    /// </summary>
    /// <param name="serverSettingService">The server setting service.</param>
    public TranscodeSettingsService(IServerSettingService serverSettingService)
    {
        this.serverSettingService = serverSettingService;
        this.cachedSettings = new TranscodeSettings();
        this.ReloadSettingsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the current transcode settings.
    /// </summary>
    public TranscodeSettings CurrentSettings => this.cachedSettings;

    /// <summary>
    /// Reloads transcode settings from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReloadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await this.reloadLock.WaitAsync(cancellationToken);
        try
        {
            var dashVideoCodec = await this.serverSettingService.GetAsync(
                ServerSettingKeys.DashVideoCodec,
                ServerSettingDefaults.DashVideoCodec,
                cancellationToken
            );

            var dashAudioCodec = await this.serverSettingService.GetAsync(
                ServerSettingKeys.DashAudioCodec,
                ServerSettingDefaults.DashAudioCodec,
                cancellationToken
            );

            var dashSegmentDuration = await this.serverSettingService.GetAsync(
                ServerSettingKeys.DashSegmentDurationSeconds,
                ServerSettingDefaults.DashSegmentDurationSeconds,
                cancellationToken
            );

            var enableToneMapping = await this.serverSettingService.GetAsync(
                ServerSettingKeys.EnableToneMapping,
                ServerSettingDefaults.EnableToneMapping,
                cancellationToken
            );

            var userPreferredAcceleration = await this.serverSettingService.GetValueAsync(
                ServerSettingKeys.UserPreferredAcceleration,
                cancellationToken
            );

            HardwareAccelerationKind? preferredAccel = null;
            if (!string.IsNullOrWhiteSpace(userPreferredAcceleration) &&
                Enum.TryParse<HardwareAccelerationKind>(userPreferredAcceleration, out var parsed))
            {
                preferredAccel = parsed;
            }

            this.cachedSettings = new TranscodeSettings
            {
                DashVideoCodec = dashVideoCodec,
                DashAudioCodec = dashAudioCodec,
                DashSegmentDurationSeconds = dashSegmentDuration,
                EnableToneMapping = enableToneMapping,
                UserPreferredAcceleration = preferredAccel,
            };
        }
        finally
        {
            this.reloadLock.Release();
        }
    }

    /// <summary>
    /// Disposes the resources used by the service.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the service.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.reloadLock.Dispose();
        }
    }
}
