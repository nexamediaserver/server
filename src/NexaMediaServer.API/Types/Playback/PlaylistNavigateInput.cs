// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for navigating to next/previous item in a playlist.
/// </summary>
public sealed class PlaylistNavigateInput
{
    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }
}
