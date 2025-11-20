// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a filesystem entry within a directory listing.
/// </summary>
public sealed class FileSystemEntryDto
{
    /// <summary>
    /// Gets the display name of the entry.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the absolute path to the entry.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is a file.
    /// </summary>
    public bool IsFile { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is a symbolic link or reparse point.
    /// </summary>
    public bool IsSymbolicLink { get; init; }

    /// <summary>
    /// Gets a value indicating whether the UI should allow selection of this entry.
    /// </summary>
    public bool IsSelectable { get; init; }
}
