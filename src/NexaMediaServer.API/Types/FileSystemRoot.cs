// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL type representing an available filesystem root.
/// </summary>
public sealed class FileSystemRoot
{
    /// <summary>
    /// Gets the identifier for the root entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the raw path accessible by the server OS.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the kind of root.
    /// </summary>
    public FileSystemRootKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the root is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Creates an API type from the corresponding core DTO.
    /// </summary>
    /// <param name="dto">The source DTO.</param>
    /// <returns>The API model.</returns>
    internal static FileSystemRoot FromDto(FileSystemRootDto dto)
    {
        return new FileSystemRoot
        {
            Id = dto.Id,
            Label = dto.Label,
            Path = dto.Path,
            Kind = dto.Kind,
            IsReadOnly = dto.IsReadOnly,
        };
    }
}
