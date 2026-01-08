// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Defines the layout type for field groups on item detail pages.
/// </summary>
public enum DetailFieldGroupLayoutType
{
    /// <summary>
    /// Fields are arranged vertically in a single column.
    /// </summary>
    Vertical = 0,

    /// <summary>
    /// Fields are arranged horizontally in a single row.
    /// </summary>
    Horizontal = 1,

    /// <summary>
    /// Fields are arranged in a responsive grid layout.
    /// </summary>
    Grid = 2,
}
