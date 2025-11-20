// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a BIF (Base Index Frames) file structure for video trickplay/scrubbing.
/// </summary>
public sealed class BifFile
{
    /// <summary>
    /// Gets or sets the BIF format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets the list of frame entries in chronological order.
    /// </summary>
    public List<BifEntry> Entries { get; } = new();

    /// <summary>
    /// Gets the image data for all frames.
    /// The key is the timestamp in milliseconds, the value is the JPEG image bytes.
    /// </summary>
    public Dictionary<int, byte[]> ImageData { get; } = new();
}
