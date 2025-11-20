// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a folder within a media library.
/// </summary>
public class SectionLocation : AuditableEntity
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
    /// Gets or sets the collection of media items in this folder.
    /// </summary>
    public ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();

    /// <summary>
    /// Gets or sets the path of the library folder.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the folder is currently available.
    /// </summary>
    public bool Available { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the folder was last scanned.
    /// </summary>
    public DateTime LastScannedAt { get; set; }
}
