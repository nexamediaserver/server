// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Represents a file that has been scanned.
/// </summary>
public class ScannedFile
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }
}
