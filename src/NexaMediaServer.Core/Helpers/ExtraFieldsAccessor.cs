// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace NexaMediaServer.Core.Helpers;

/// <summary>
/// Provides type-safe access to <see cref="Entities.MetadataItem.ExtraFields"/> dictionary values.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ExtraFields</c> dictionary stores dynamic metadata as JSON elements. This helper
/// provides typed getter/setter methods with proper null handling, type coercion, and
/// validation logging for unexpected types.
/// </para>
/// <para>
/// All getters return <c>null</c> or default values when the key is missing or the value
/// cannot be coerced to the expected type. Validation failures are logged but do not throw.
/// </para>
/// </remarks>
public static class ExtraFieldsAccessor
{
    /// <summary>
    /// Gets a string value from the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <returns>The string value, or <c>null</c> if not present or not a string.</returns>
    public static string? GetString(
        IReadOnlyDictionary<string, JsonElement>? extraFields,
        string key)
    {
        if (extraFields is null || !extraFields.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            JsonValueKind.Null => null,
            _ => null,
        };
    }

    /// <summary>
    /// Gets an integer value from the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <returns>The integer value, or <c>null</c> if not present or not convertible.</returns>
    public static int? GetInt(
        IReadOnlyDictionary<string, JsonElement>? extraFields,
        string key)
    {
        if (extraFields is null || !extraFields.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
            JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    /// <summary>
    /// Gets a boolean value from the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <returns>The boolean value, or <c>null</c> if not present or not convertible.</returns>
    public static bool? GetBool(
        IReadOnlyDictionary<string, JsonElement>? extraFields,
        string key)
    {
        if (extraFields is null || !extraFields.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal != 0,
            JsonValueKind.String => ParseBoolString(element.GetString()),
            _ => null,
        };
    }

    /// <summary>
    /// Gets a string array value from the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <returns>The string array, or <c>null</c> if not present or not an array.</returns>
    public static string[]? GetStringArray(
        IReadOnlyDictionary<string, JsonElement>? extraFields,
        string key)
    {
        if (extraFields is null || !extraFields.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        // Single string value - split by semicolon for multi-value fields
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                return str
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }
        }

        return null;
    }

    /// <summary>
    /// Sets a string value in the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <param name="value">The value to set, or <c>null</c> to remove the key.</param>
    public static void SetString(
        Dictionary<string, JsonElement> extraFields,
        string key,
        string? value)
    {
        ArgumentNullException.ThrowIfNull(extraFields);

        if (string.IsNullOrWhiteSpace(value))
        {
            extraFields.Remove(key);
        }
        else
        {
            extraFields[key] = JsonSerializer.SerializeToElement(value.Trim());
        }
    }

    /// <summary>
    /// Sets an integer value in the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <param name="value">The value to set, or <c>null</c> to remove the key.</param>
    public static void SetInt(
        Dictionary<string, JsonElement> extraFields,
        string key,
        int? value)
    {
        ArgumentNullException.ThrowIfNull(extraFields);

        if (value is null)
        {
            extraFields.Remove(key);
        }
        else
        {
            extraFields[key] = JsonSerializer.SerializeToElement(value.Value);
        }
    }

    /// <summary>
    /// Sets a boolean value in the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <param name="value">The value to set, or <c>null</c> to remove the key.</param>
    public static void SetBool(
        Dictionary<string, JsonElement> extraFields,
        string key,
        bool? value)
    {
        ArgumentNullException.ThrowIfNull(extraFields);

        if (value is null)
        {
            extraFields.Remove(key);
        }
        else
        {
            extraFields[key] = JsonSerializer.SerializeToElement(value.Value);
        }
    }

    /// <summary>
    /// Sets a string array value in the extra fields dictionary.
    /// </summary>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <param name="values">The values to set, or <c>null</c>/empty to remove the key.</param>
    public static void SetStringArray(
        Dictionary<string, JsonElement> extraFields,
        string key,
        IEnumerable<string>? values)
    {
        ArgumentNullException.ThrowIfNull(extraFields);

        var filtered = values?
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToArray();

        if (filtered is null || filtered.Length == 0)
        {
            extraFields.Remove(key);
        }
        else
        {
            extraFields[key] = JsonSerializer.SerializeToElement(filtered);
        }
    }

    /// <summary>
    /// Tries to get a value with type validation.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="extraFields">The extra fields dictionary.</param>
    /// <param name="key">The field key.</param>
    /// <param name="value">The extracted value if successful.</param>
    /// <returns><c>true</c> if the value was found and converted successfully.</returns>
    public static bool TryGetValue<T>(
        IReadOnlyDictionary<string, JsonElement>? extraFields,
        string key,
        [NotNullWhen(true)] out T? value)
    {
        value = default;

        if (extraFields is null || !extraFields.TryGetValue(key, out var element))
        {
            return false;
        }

        try
        {
            var result = typeof(T) switch
            {
                var t when t == typeof(string) => (T?)(object?)GetStringFromElement(element),
                var t when t == typeof(int) || t == typeof(int?) => (T?)(object?)GetIntFromElement(element),
                var t when t == typeof(bool) || t == typeof(bool?) => (T?)(object?)GetBoolFromElement(element),
                var t when t == typeof(string[]) => (T?)(object?)GetStringArrayFromElement(element),
                _ => element.Deserialize<T>(),
            };

            if (result is not null)
            {
                value = result;
                return true;
            }
        }
        catch (JsonException)
        {
            // Failed to deserialize - return false without value
        }

        return false;
    }

    private static bool? ParseBoolString(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        return str.Trim().ToUpperInvariant() switch
        {
            "1" or "TRUE" or "YES" or "ON" => true,
            "0" or "FALSE" or "NO" or "OFF" => false,
            _ => null,
        };
    }

    private static string? GetStringFromElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => null,
        };
    }

    private static int? GetIntFromElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
            JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static bool? GetBoolFromElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal != 0,
            JsonValueKind.String => ParseBoolString(element.GetString()),
            _ => null,
        };
    }

    private static string[]? GetStringArrayFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        return null;
    }
}
