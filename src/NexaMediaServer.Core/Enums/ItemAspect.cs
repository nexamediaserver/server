// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the aspect ratio for displaying items in hubs.
/// </summary>
public enum ItemAspect
{
    /// <summary>
    /// Poster aspect ratio (2:3), used for movies, shows, books.
    /// </summary>
    Poster = 1,

    /// <summary>
    /// Square aspect ratio (1:1), used for albums, tracks.
    /// </summary>
    Square = 2,

    /// <summary>
    /// Wide/landscape aspect ratio (16:9), used for extras, clips.
    /// </summary>
    Wide = 3,
}
