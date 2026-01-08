// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Implementation of tag moderation service.
/// </summary>
public class TagModerationService : ITagModerationService, IDisposable
{
    private readonly IServerSettingService serverSettingService;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private List<string> allowedTags = [];
    private List<string> blockedTags = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TagModerationService"/> class.
    /// </summary>
    /// <param name="serverSettingService">The server setting service.</param>
    public TagModerationService(IServerSettingService serverSettingService)
    {
        this.serverSettingService = serverSettingService;
        this.ReloadSettingsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reloads tag moderation settings from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReloadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await this.reloadLock.WaitAsync(cancellationToken);
        try
        {
            var allowedJson = await this.serverSettingService.GetValueAsync(
                ServerSettingKeys.AllowedTags,
                cancellationToken
            );
            var blockedJson = await this.serverSettingService.GetValueAsync(
                ServerSettingKeys.BlockedTags,
                cancellationToken
            );

            this.allowedTags = !string.IsNullOrWhiteSpace(allowedJson)
                ? JsonSerializer.Deserialize<List<string>>(allowedJson) ?? []
                : [];

            this.blockedTags = !string.IsNullOrWhiteSpace(blockedJson)
                ? JsonSerializer.Deserialize<List<string>>(blockedJson) ?? []
                : [];
        }
        finally
        {
            this.reloadLock.Release();
        }
    }

    /// <inheritdoc/>
    public bool IsTagAllowed(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        // If allowlist has entries, use it exclusively (ignore blocklist)
        if (this.allowedTags.Count > 0)
        {
            return this.allowedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase);
        }

        // If blocklist has entries, reject matching tags
        if (this.blockedTags.Count > 0)
        {
            return !this.blockedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase);
        }

        // No moderation configured - allow all tags
        return true;
    }

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the service and releases resources.
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
