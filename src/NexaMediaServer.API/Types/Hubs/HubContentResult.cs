// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types.Hubs;

/// <summary>
/// Represents the content result for a hub.
/// </summary>
[GraphQLName("HubContent")]
public sealed class HubContentResult
{
    /// <summary>
    /// Gets the hub key this content belongs to.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the metadata items in this hub.
    /// </summary>
    public IReadOnlyList<MetadataItem> Items { get; init; } = [];
}
