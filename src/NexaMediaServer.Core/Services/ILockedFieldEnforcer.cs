// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides methods to check and enforce field locking on <see cref="MetadataItem"/> entities.
/// </summary>
/// <remarks>
/// Field locking allows users to prevent automatic metadata updates from overwriting
/// manually-set values. This service provides utilities to check lock status and
/// conditionally apply updates based on lock state.
/// </remarks>
public interface ILockedFieldEnforcer
{
    /// <summary>
    /// Determines whether a specific field is locked on a metadata item.
    /// </summary>
    /// <param name="item">The metadata item to check.</param>
    /// <param name="fieldName">The name of the field to check (use constants from <see cref="Constants.MetadataFieldNames"/>).</param>
    /// <param name="overrideFields">Optional collection of field names that should be treated as unlocked regardless of lock status.</param>
    /// <returns><c>true</c> if the field is locked and not in the override list; otherwise, <c>false</c>.</returns>
    bool IsFieldLocked(MetadataItem item, string fieldName, IEnumerable<string>? overrideFields = null);

    /// <summary>
    /// Assigns a value to a metadata item property if the field is not locked.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="item">The metadata item to update.</param>
    /// <param name="fieldName">The name of the field being updated (use constants from <see cref="Constants.MetadataFieldNames"/>).</param>
    /// <param name="currentValue">The current value of the property.</param>
    /// <param name="newValue">The new value to assign if the field is unlocked and the value differs.</param>
    /// <param name="setter">Action to set the property value on the item.</param>
    /// <param name="overrideFields">Optional collection of field names that should be treated as unlocked regardless of lock status.</param>
    /// <returns><c>true</c> if the value was changed; otherwise, <c>false</c>.</returns>
    bool AssignIfUnlocked<T>(
        MetadataItem item,
        string fieldName,
        T? currentValue,
        T? newValue,
        Action<T?> setter,
        IEnumerable<string>? overrideFields = null);

    /// <summary>
    /// Assigns a string value to a metadata item property if the field is not locked,
    /// preferring non-empty values.
    /// </summary>
    /// <param name="item">The metadata item to update.</param>
    /// <param name="fieldName">The name of the field being updated (use constants from <see cref="Constants.MetadataFieldNames"/>).</param>
    /// <param name="currentValue">The current value of the property.</param>
    /// <param name="newValue">The new value to assign if the field is unlocked and the value differs.</param>
    /// <param name="setter">Action to set the property value on the item.</param>
    /// <param name="overrideFields">Optional collection of field names that should be treated as unlocked regardless of lock status.</param>
    /// <returns><c>true</c> if the value was changed; otherwise, <c>false</c>.</returns>
    bool AssignStringIfUnlocked(
        MetadataItem item,
        string fieldName,
        string? currentValue,
        string? newValue,
        Action<string?> setter,
        IEnumerable<string>? overrideFields = null);
}
