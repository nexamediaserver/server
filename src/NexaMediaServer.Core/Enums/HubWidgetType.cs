// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the recommended widget type for rendering a hub on the client.
/// </summary>
public enum HubWidgetType
{
    /// <summary>
    /// A horizontal slider of items or people cards.
    /// </summary>
    Slider = 1,

    /// <summary>
    /// A timeline list of items ordered from most recent to least recent.
    /// </summary>
    Timeline = 2,

    /// <summary>
    /// A large hero carousel with backdrop images, logos, and rich metadata.
    /// </summary>
    Hero = 3,
}
