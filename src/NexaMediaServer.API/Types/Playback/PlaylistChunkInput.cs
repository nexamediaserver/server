// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used to request a chunk of playlist items.
/// </summary>
public sealed class PlaylistChunkInput
{
    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the starting index (0-based).
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
