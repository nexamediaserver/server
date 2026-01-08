// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for refreshing metadata for a single item (optionally including descendants).
/// </summary>
public sealed class RefreshItemMetadataInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier.
    /// </summary>
    [ID]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include all descendants.
    /// Defaults to true to refresh entire item trees.
    /// </summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>
    /// Gets or sets optional field names to force update, bypassing any locks.
    /// Use constants from MetadataFieldNames for built-in fields.
    /// When not specified or empty, locked fields are respected.
    /// </summary>
    public IReadOnlyList<string>? OverrideFields { get; set; }
}
