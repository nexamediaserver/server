// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents pagination parameters for cursor-based pagination.
/// </summary>
public class PaginationParameters
{
    /// <summary>
    /// Gets or sets the number of items to retrieve from the start.
    /// </summary>
    public int? First { get; set; }

    /// <summary>
    /// Gets or sets the cursor to retrieve items after.
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// Gets or sets the number of items to retrieve from the end.
    /// </summary>
    public int? Last { get; set; }

    /// <summary>
    /// Gets or sets the cursor to retrieve items before.
    /// </summary>
    public string? Before { get; set; }
}
