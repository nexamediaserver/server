// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Implementation of genre normalization service.
/// </summary>
public class GenreNormalizationService : IGenreNormalizationService, IDisposable
{
    private readonly IServerSettingService serverSettingService;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private Dictionary<string, string> mappings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GenreNormalizationService"/> class.
    /// </summary>
    /// <param name="serverSettingService">The server setting service.</param>
    public GenreNormalizationService(IServerSettingService serverSettingService)
    {
        this.serverSettingService = serverSettingService;
        this.ReloadSettingsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reloads genre normalization mappings from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReloadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await this.reloadLock.WaitAsync(cancellationToken);
        try
        {
            var mappingsJson = await this.serverSettingService.GetValueAsync(
                ServerSettingKeys.GenreMappings,
                cancellationToken
            );

            this.mappings = !string.IsNullOrWhiteSpace(mappingsJson)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(mappingsJson)
                    ?? new Dictionary<string, string>()
                : new Dictionary<string, string>();
        }
        finally
        {
            this.reloadLock.Release();
        }
    }

    /// <inheritdoc/>
    public string NormalizeGenreName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Check for exact match first
        if (this.mappings.TryGetValue(input, out var normalized))
        {
            return normalized;
        }

        // Check case-insensitive match
        var match = this.mappings.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, input, StringComparison.OrdinalIgnoreCase)
        );

        return !string.IsNullOrEmpty(match.Value) ? match.Value : input;
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
