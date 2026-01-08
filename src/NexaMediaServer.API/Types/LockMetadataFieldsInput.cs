// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for locking fields on a metadata item.
/// </summary>
/// <param name="ItemId">The UUID of the metadata item to lock fields on.</param>
/// <param name="Fields">The field names to lock. Use constants from MetadataFieldNames for built-in fields.</param>
public record LockMetadataFieldsInput(
    [property: ID("Item")]
    Guid ItemId,
    IReadOnlyList<string> Fields
);
