// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Provides methods for accessing and managing server settings in the database.
/// </summary>
public interface IServerSettingRepository
{
    /// <summary>
    /// Gets a setting by its unique key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The server setting if found; otherwise, null.</returns>
    Task<ServerSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all server settings.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all server settings.</returns>
    Task<IReadOnlyList<ServerSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a server setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a server setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the setting was deleted; false if it did not exist.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
