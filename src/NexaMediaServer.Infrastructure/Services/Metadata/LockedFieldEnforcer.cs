// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Provides methods to check and enforce field locking on <see cref="MetadataItem"/> entities.
/// </summary>
public sealed class LockedFieldEnforcer : ILockedFieldEnforcer
{
    /// <inheritdoc/>
    public bool IsFieldLocked(MetadataItem item, string fieldName, IEnumerable<string>? overrideFields = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        // If the field is in the override list, it's not considered locked
        if (overrideFields?.Contains(fieldName, StringComparer.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        // Check if the field is in the locked fields collection
        return item.LockedFields?.Contains(fieldName, StringComparer.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc/>
    public bool AssignIfUnlocked<T>(
        MetadataItem item,
        string fieldName,
        T? currentValue,
        T? newValue,
        Action<T?> setter,
        IEnumerable<string>? overrideFields = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(setter);

        if (this.IsFieldLocked(item, fieldName, overrideFields))
        {
            return false;
        }

        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setter(newValue);
        return true;
    }

    /// <inheritdoc/>
    public bool AssignStringIfUnlocked(
        MetadataItem item,
        string fieldName,
        string? currentValue,
        string? newValue,
        Action<string?> setter,
        IEnumerable<string>? overrideFields = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(setter);

        if (this.IsFieldLocked(item, fieldName, overrideFields))
        {
            return false;
        }

        // Prefer non-empty string values
        var preferredValue = PreferString(newValue, currentValue);

        if (string.Equals(currentValue, preferredValue, StringComparison.Ordinal))
        {
            return false;
        }

        setter(preferredValue);
        return true;
    }

    /// <summary>
    /// Returns the preferred string value, preferring non-empty strings.
    /// </summary>
    private static string? PreferString(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
}
