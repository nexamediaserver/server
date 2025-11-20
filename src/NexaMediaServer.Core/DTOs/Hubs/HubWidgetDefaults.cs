// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// Provides sensible default widget recommendations for each hub type.
/// </summary>
public static class HubWidgetDefaults
{
    /// <summary>
    /// Gets the recommended widget type for a given hub type.
    /// </summary>
    /// <param name="hubType">The hub type to get the default widget for.</param>
    /// <returns>The recommended widget type.</returns>
    /// <remarks>
    /// Currently all hubs default to Slider. This method provides a central
    /// location to customize widget recommendations per hub type in the future.
    /// For example, RecentlyAdded could use Timeline and featured content could use Hero.
    /// </remarks>
    public static HubWidgetType GetDefaultWidget(HubType hubType)
    {
        // All current hubs use Slider
        // When customizing, add specific cases before the default:
        // HubType.RecentlyAdded => HubWidgetType.Timeline,
        // HubType.Featured => HubWidgetType.Hero,
        _ = hubType; // Suppress unused parameter warning until cases are added
        return HubWidgetType.Slider;
    }
}
