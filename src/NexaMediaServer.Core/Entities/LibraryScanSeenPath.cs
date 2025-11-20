// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a file path that was seen during a library scan.
/// Used for checkpoint persistence and orphan detection.
/// </summary>
public class LibraryScanSeenPath
{
    /// <summary>
    /// Gets or sets the ID of the parent library scan.
    /// </summary>
    public int LibraryScanId { get; set; }

    /// <summary>
    /// Gets or sets the parent library scan entity.
    /// </summary>
    public LibraryScan LibraryScan { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file path that was seen during the scan.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the path was recorded.
    /// </summary>
    public DateTime SeenAt { get; set; }
}
