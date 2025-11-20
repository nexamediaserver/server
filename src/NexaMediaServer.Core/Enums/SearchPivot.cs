// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Specifies the type of content to search for in the search index.
/// </summary>
public enum SearchPivot
{
    /// <summary>
    /// Returns top results across all metadata item types.
    /// </summary>
    Top,

    /// <summary>
    /// Returns only movie results.
    /// </summary>
    Movie,

    /// <summary>
    /// Returns only TV show results.
    /// </summary>
    Show,

    /// <summary>
    /// Returns only episode results.
    /// </summary>
    Episode,

    /// <summary>
    /// Returns only person and group results.
    /// </summary>
    People,

    /// <summary>
    /// Returns only music album results.
    /// </summary>
    Album,

    /// <summary>
    /// Returns only music track results.
    /// </summary>
    Track,
}
