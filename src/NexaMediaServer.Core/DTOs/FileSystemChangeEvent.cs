// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a filesystem change event detected by a library watcher.
/// </summary>
public sealed class FileSystemChangeEvent
{
    /// <summary>
    /// Gets the library section ID where the change was detected.
    /// </summary>
    public required int LibrarySectionId { get; init; }

    /// <summary>
    /// Gets the section location ID where the change was detected.
    /// </summary>
    public required int SectionLocationId { get; init; }

    /// <summary>
    /// Gets the type of filesystem change.
    /// </summary>
    public required FileSystemChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the full path of the affected file or directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the previous path for rename events.
    /// </summary>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the affected path is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Gets the timestamp when the event was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}
