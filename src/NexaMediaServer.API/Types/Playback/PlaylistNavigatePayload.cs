// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload returned after navigating in a playlist or changing modes.
/// </summary>
public sealed class PlaylistNavigatePayload
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the current playlist item after navigation.
    /// </summary>
    public PlaylistItemPayload? CurrentItem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled.
    /// </summary>
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode is enabled.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Gets or sets the current cursor position.
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// Gets or sets the total count of items in the playlist.
    /// </summary>
    public int TotalCount { get; set; }
}
