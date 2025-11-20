// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a top-level filesystem root, such as a drive, mount, or volume.
/// </summary>
public sealed class FileSystemRootDto
{
    /// <summary>
    /// Gets a stable identifier for the root.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-friendly label displayed to users.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the raw path value expected by the underlying platform.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the kind of root for filtering or iconography.
    /// </summary>
    public FileSystemRootKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the root is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }
}
