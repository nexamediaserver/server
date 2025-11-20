// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration options for trickplay BIF file generation.
/// </summary>
public class TrickplayOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Trickplay";

    /// <summary>
    /// Gets or sets the interval in milliseconds between snapshots.
    /// Default is 2000ms (2 seconds).
    /// </summary>
    public int SnapshotIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum width for trickplay thumbnails in pixels.
    /// Height is automatically calculated to preserve aspect ratio.
    /// Default is 320 pixels.
    /// </summary>
    public int MaxSnapshotWidth { get; set; } = 320;

    /// <summary>
    /// Gets or sets the JPEG quality for snapshots (1-100).
    /// Higher values produce better quality but larger file sizes.
    /// Default is 85.
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// Gets or sets a value indicating whether to skip BIF generation if the file already exists.
    /// Default is true.
    /// </summary>
    public bool SkipExisting { get; set; } = true;
}
