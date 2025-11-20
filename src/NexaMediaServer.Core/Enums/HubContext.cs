// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the context in which a hub is displayed.
/// </summary>
public enum HubContext
{
    /// <summary>
    /// Hub is displayed on the global home page, aggregating content from all user-accessible libraries.
    /// </summary>
    Home = 1,

    /// <summary>
    /// Hub is displayed on the library-specific discover page.
    /// </summary>
    LibraryDiscover = 2,

    /// <summary>
    /// Hub is displayed on a metadata item's detail page, showing related content.
    /// </summary>
    ItemDetail = 3,
}
