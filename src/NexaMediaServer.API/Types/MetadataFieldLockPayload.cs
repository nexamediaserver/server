// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after locking or unlocking metadata fields.
/// </summary>
/// <param name="Success">Whether the operation was successful.</param>
/// <param name="LockedFields">The current list of locked fields on the item after the operation.</param>
/// <param name="Error">Error message if the operation failed.</param>
public record MetadataFieldLockPayload(
    bool Success,
    IReadOnlyList<string>? LockedFields = null,
    string? Error = null
);
