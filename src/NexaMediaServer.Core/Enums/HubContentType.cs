// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of content a hub displays, determining how it should be rendered.
/// </summary>
public enum HubContentType
{
    /// <summary>
    /// Hub displays metadata items (movies, shows, episodes, albums, tracks, etc.).
    /// Should be rendered using ItemSlider.
    /// </summary>
    Items = 1,

    /// <summary>
    /// Hub displays people (cast, crew, artists).
    /// Should be rendered using RoleSlider.
    /// </summary>
    People = 2,
}
