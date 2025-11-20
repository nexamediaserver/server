// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a folder within a media library.
/// </summary>
public class Directory : SoftDeletableEntity
{
    /// <summary>
    /// Gets or sets the ID of the library this folder belongs to.
    /// </summary>
    public int LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library this folder belongs to.
    /// </summary>
    public LibrarySection LibrarySection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the parent directory.
    /// </summary>
    public int? ParentDirectoryId { get; set; }

    /// <summary>
    /// Gets or sets the parent directory.
    /// </summary>
    public Directory? ParentDirectory { get; set; }

    /// <summary>
    /// Gets or sets the collection of subdirectories in this folder.
    /// </summary>
    public ICollection<Directory> SubDirectories { get; set; } = new List<Directory>();

    /// <summary>
    /// Gets or sets the relative path of the directory.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
