// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for setting shuffle or repeat mode on a playlist.
/// </summary>
public sealed class PlaylistModeInput
{
    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mode should be enabled.
    /// </summary>
    public bool Enabled { get; set; }
}
