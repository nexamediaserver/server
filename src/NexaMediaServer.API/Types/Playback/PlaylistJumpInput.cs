// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for jumping to a specific index in the playlist.
/// </summary>
public sealed class PlaylistJumpInput
{
    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the 0-based index to jump to.
    /// </summary>
    public int Index { get; set; }
}
