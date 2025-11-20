// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a paged (fixed-size) directory listing response.
/// </summary>
public sealed class DirectoryListingDto
{
    /// <summary>
    /// Gets the canonical path of the directory that was listed.
    /// </summary>
    public required string CurrentPath { get; init; }

    /// <summary>
    /// Gets the parent directory path if one exists.
    /// </summary>
    public string? ParentPath { get; init; }

    /// <summary>
    /// Gets the entries inside the directory.
    /// </summary>
    public IReadOnlyList<FileSystemEntryDto> Entries { get; init; } =
        Array.Empty<FileSystemEntryDto>();
}
