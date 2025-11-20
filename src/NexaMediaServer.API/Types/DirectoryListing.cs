// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL type representing a directory listing response.
/// </summary>
public sealed class DirectoryListing
{
    /// <summary>
    /// Gets the canonical path that was listed.
    /// </summary>
    public required string CurrentPath { get; init; }

    /// <summary>
    /// Gets the parent path if available.
    /// </summary>
    public string? ParentPath { get; init; }

    /// <summary>
    /// Gets the child entries.
    /// </summary>
    public IReadOnlyList<FileSystemEntry> Entries { get; init; } = Array.Empty<FileSystemEntry>();

    /// <summary>
    /// Creates an API directory listing from the DTO.
    /// </summary>
    /// <param name="dto">The source DTO.</param>
    /// <returns>The API directory listing.</returns>
    internal static DirectoryListing FromDto(DirectoryListingDto dto)
    {
        return new DirectoryListing
        {
            CurrentPath = dto.CurrentPath,
            ParentPath = dto.ParentPath,
            Entries = dto.Entries.Select(FileSystemEntry.FromDto).ToList(),
        };
    }
}
