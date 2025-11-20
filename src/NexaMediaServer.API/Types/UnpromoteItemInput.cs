// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for unpromoting a metadata item from the hero carousel.
/// </summary>
public sealed class UnpromoteItemInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier to unpromote.
    /// </summary>
    [ID]
    public Guid ItemId { get; set; }
}
