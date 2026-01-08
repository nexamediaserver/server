// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for unlocking fields on a metadata item.
/// </summary>
/// <param name="ItemId">The UUID of the metadata item to unlock fields on.</param>
/// <param name="Fields">The field names to unlock.</param>
public record UnlockMetadataFieldsInput(
    [property: ID("Item")]
    Guid ItemId,
    IReadOnlyList<string> Fields
);
