// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the widget type for rendering a field on the client.
/// </summary>
public enum DetailFieldWidgetType
{
    /// <summary>
    /// Plain text display.
    /// </summary>
    Text = 1,

    /// <summary>
    /// A heading/title display (typically larger, bold text).
    /// </summary>
    Heading = 2,

    /// <summary>
    /// A list of items (e.g., genres, tags).
    /// </summary>
    List = 3,

    /// <summary>
    /// A badge/pill display (e.g., content rating).
    /// </summary>
    Badge = 4,

    /// <summary>
    /// A clickable link.
    /// </summary>
    Link = 5,

    /// <summary>
    /// A formatted date display.
    /// </summary>
    Date = 6,

    /// <summary>
    /// A formatted duration display (e.g., "2h 15m").
    /// </summary>
    Duration = 7,

    /// <summary>
    /// A numeric value display.
    /// </summary>
    Number = 8,

    /// <summary>
    /// A boolean/toggle display.
    /// </summary>
    Boolean = 9,

    /// <summary>
    /// The actions button block (Play, Edit, Menu, etc.).
    /// The client determines which buttons to render based on user role and item capabilities.
    /// </summary>
    Actions = 10,
}
