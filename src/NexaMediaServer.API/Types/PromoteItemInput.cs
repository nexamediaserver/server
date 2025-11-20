// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for promoting a metadata item to the hero carousel.
/// </summary>
public sealed class PromoteItemInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier to promote.
    /// </summary>
    [ID]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the optional expiration date for the promotion.
    /// If set, the item will be automatically unpromoted after this time.
    /// </summary>
    public DateTime? PromotedUntil { get; set; }
}
