// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after updating a metadata item.
/// </summary>
/// <param name="Success">Whether the operation was successful.</param>
/// <param name="Item">The updated metadata item.</param>
/// <param name="Error">Error message if the operation failed.</param>
public record UpdateMetadataItemPayload(
    bool Success,
    MetadataItem? Item = null,
    string? Error = null
);
