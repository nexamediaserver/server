// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a media library section entity.
/// </summary>
public class LibrarySection : AuditableEntity
{
    /// <summary>
    /// Gets or sets the UUID of the library.
    /// </summary>
    public Guid Uuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the library.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort name of the library.
    /// </summary>
    public string SortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the library.
    /// </summary>
    public LibraryType Type { get; set; }

    /// <summary>
    /// Gets or sets the collection of root library folders.
    /// </summary>
    public List<SectionLocation> Locations { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of root directories in the library.
    /// </summary>
    public List<Directory> Directories { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of root metadata items in the library.
    /// </summary>
    public List<MetadataItem> MetadataItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of library scans.
    /// </summary>
    public List<LibraryScan> Scans { get; set; } = [];

    /// <summary>
    /// Gets or sets the date and time when the library was last scanned.
    /// </summary>
    public DateTime LastScannedAt { get; set; }

    /// <summary>
    /// Gets or sets the library-specific settings (metadata preferences, per-agent configuration, etc.).
    /// </summary>
    public LibrarySectionSetting Settings { get; set; } = new();
}
