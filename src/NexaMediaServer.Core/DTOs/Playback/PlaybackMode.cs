// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Playback delivery mode in priority order.
/// </summary>
public enum PlaybackMode
{
    /// <summary>
    /// Send the file byte-for-byte as stored on disk.
    /// </summary>
    DirectPlay = 0,

    /// <summary>
    /// Repackage tracks into a different container without re-encoding.
    /// </summary>
    DirectStream = 1,

    /// <summary>
    /// Transcode incompatible tracks and segment into a manifest.
    /// </summary>
    Transcode = 2,
}
