// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents filter input for querying metadata items.
/// </summary>
public class MetadataItemFilterInput
{
    /// <summary>
    /// Gets or sets the search query for filtering metadata items.
    /// </summary>
    public string? SearchQuery { get; set; }
}
