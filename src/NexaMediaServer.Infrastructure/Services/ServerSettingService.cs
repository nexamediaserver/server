// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service for managing server-wide settings with fallback to IConfiguration and constants.
/// </summary>
public partial class ServerSettingService : IServerSettingService
{
    private const string ConfigurationSectionName = "ServerDefaults";

    private readonly IServerSettingRepository repository;
    private readonly IConfiguration configuration;
    private readonly ILogger<ServerSettingService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSettingService"/> class.
    /// </summary>
    /// <param name="repository">The server setting repository.</param>
    /// <param name="configuration">The application configuration for fallback values.</param>
    /// <param name="logger">The logger.</param>
    public ServerSettingService(
        IServerSettingRepository repository,
        IConfiguration configuration,
        ILogger<ServerSettingService> logger)
    {
        this.repository = repository;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        // First, try database
        var dbValue = await this.repository.GetByKeyAsync(key, cancellationToken);
        if (dbValue is not null)
        {
            try
            {
                return DeserializeValue<T>(dbValue.Value);
            }
            catch (Exception ex)
            {
                this.LogDeserializationError(key, ex);
            }
        }

        // Second, try IConfiguration fallback
        var configValue = this.configuration[$"{ConfigurationSectionName}:{key}"];
        if (!string.IsNullOrEmpty(configValue))
        {
            try
            {
                return DeserializeValue<T>(configValue);
            }
            catch (Exception ex)
            {
                this.LogConfigurationFallbackError(key, ex);
            }
        }

        // Third, return provided default
        return defaultValue;
    }

    /// <inheritdoc />
    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var setting = await this.repository.GetByKeyAsync(key, cancellationToken);
        if (setting is not null)
        {
            return setting.Value;
        }

        // Fallback to IConfiguration
        return this.configuration[$"{ConfigurationSectionName}:{key}"];
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        var serialized = SerializeValue(value);
        await this.repository.UpsertAsync(key, serialized, cancellationToken);
        this.LogSettingUpdated(key);
    }

    /// <inheritdoc />
    public async Task SetValueAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        await this.repository.UpsertAsync(key, value, cancellationToken);
        this.LogSettingUpdated(key);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return this.repository.DeleteAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await this.repository.GetAllAsync(cancellationToken);
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    private static T DeserializeValue<T>(string value)
    {
        var targetType = typeof(T);

        // Handle primitive types directly
        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }

        if (targetType == typeof(int))
        {
            return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(long))
        {
            return (T)(object)long.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        if (targetType == typeof(double))
        {
            return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(decimal))
        {
            return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        // For complex types, use JSON deserialization
        return JsonSerializer.Deserialize<T>(value)
            ?? throw new InvalidOperationException($"Failed to deserialize value for type {targetType.Name}");
    }

    private static string SerializeValue<T>(T value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var targetType = typeof(T);

        // Handle primitive types directly
        if (targetType == typeof(string))
        {
            return (string)(object)value;
        }

        if (targetType == typeof(int)
            || targetType == typeof(long)
            || targetType == typeof(double)
            || targetType == typeof(decimal))
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        if (targetType == typeof(bool))
        {
            return value.ToString()?.ToLowerInvariant() ?? "false";
        }

        // For complex types, use JSON serialization
        return JsonSerializer.Serialize(value);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize setting '{Key}' from database")]
    private partial void LogDeserializationError(string key, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse configuration fallback for setting '{Key}'")]
    private partial void LogConfigurationFallbackError(string key, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Server setting '{Key}' was updated")]
    private partial void LogSettingUpdated(string key);
}
