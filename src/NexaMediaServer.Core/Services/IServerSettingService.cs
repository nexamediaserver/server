// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides methods for reading and writing server-wide configuration settings.
/// </summary>
public interface IServerSettingService
{
    /// <summary>
    /// Gets a setting value as the specified type, returning a default if not found.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the setting value to.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if the setting is not found.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The setting value or the default.</returns>
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a raw string setting value, returning null if not found.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The raw value or null if not found.</returns>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value, serializing it to JSON if not a primitive type.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a raw string setting value.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The raw string value.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the setting was deleted; false if it did not exist.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings as a dictionary of key-value pairs.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary containing all settings.</returns>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
}
