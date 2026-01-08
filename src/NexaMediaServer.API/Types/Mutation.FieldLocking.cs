// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL mutation operations for metadata field locking.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Locks specified fields on a metadata item, preventing automatic updates.
    /// </summary>
    /// <param name="input">The item and fields to lock.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success and the current locked fields.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<MetadataFieldLockPayload> LockMetadataFieldsAsync(
        LockMetadataFieldsInput input,
        [Service] MediaServerContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            return new MetadataFieldLockPayload(false, Error: "Input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            return new MetadataFieldLockPayload(false, Error: "Item ID is required.");
        }

        if (input.Fields is null || input.Fields.Count == 0)
        {
            return new MetadataFieldLockPayload(false, Error: "At least one field name is required.");
        }

        var item = await dbContext.MetadataItems
            .FirstOrDefaultAsync(m => m.Uuid == input.ItemId, cancellationToken);

        if (item is null)
        {
            return new MetadataFieldLockPayload(false, Error: $"Metadata item with ID '{input.ItemId}' not found.");
        }

        // Add new fields to the locked set (avoid duplicates)
        var currentLocked = new HashSet<string>(item.LockedFields, StringComparer.OrdinalIgnoreCase);
        var fieldsToAdd = input.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim());

        foreach (var field in fieldsToAdd)
        {
            currentLocked.Add(field);
        }

        item.LockedFields = currentLocked.ToList();

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MetadataFieldLockPayload(true, item.LockedFields.ToList());
    }

    /// <summary>
    /// Unlocks specified fields on a metadata item, allowing automatic updates.
    /// </summary>
    /// <param name="input">The item and fields to unlock.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success and the current locked fields.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<MetadataFieldLockPayload> UnlockMetadataFieldsAsync(
        UnlockMetadataFieldsInput input,
        [Service] MediaServerContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            return new MetadataFieldLockPayload(false, Error: "Input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            return new MetadataFieldLockPayload(false, Error: "Item ID is required.");
        }

        if (input.Fields is null || input.Fields.Count == 0)
        {
            return new MetadataFieldLockPayload(false, Error: "At least one field name is required.");
        }

        var item = await dbContext.MetadataItems
            .FirstOrDefaultAsync(m => m.Uuid == input.ItemId, cancellationToken);

        if (item is null)
        {
            return new MetadataFieldLockPayload(false, Error: $"Metadata item with ID '{input.ItemId}' not found.");
        }

        // Remove fields from the locked set
        var fieldsToRemove = new HashSet<string>(input.Fields, StringComparer.OrdinalIgnoreCase);
        var updatedLocked = item.LockedFields
            .Where(f => !fieldsToRemove.Contains(f))
            .ToList();

        item.LockedFields = updatedLocked;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MetadataFieldLockPayload(true, item.LockedFields.ToList());
    }
}
