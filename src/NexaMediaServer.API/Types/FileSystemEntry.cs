// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL type describing an entry in a directory listing.
/// </summary>
public sealed class FileSystemEntry
{
    /// <summary>
    /// Gets the entry name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the raw server path to the entry.
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
    /// Gets a value indicating whether the entry is a symbolic link.
    /// </summary>
    public bool IsSymbolicLink { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry can be selected in the UI.
    /// </summary>
    public bool IsSelectable { get; init; }

    /// <summary>
    /// Creates an API entry from the underlying DTO.
    /// </summary>
    /// <param name="dto">The source DTO.</param>
    /// <returns>The API entry.</returns>
    internal static FileSystemEntry FromDto(FileSystemEntryDto dto)
    {
        return new FileSystemEntry
        {
            Name = dto.Name,
            Path = dto.Path,
            IsDirectory = dto.IsDirectory,
            IsFile = dto.IsFile,
            IsSymbolicLink = dto.IsSymbolicLink,
            IsSelectable = dto.IsSelectable,
        };
    }
}
