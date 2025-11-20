// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a metadata item that contains information about media content such as movies, TV shows, seasons, or episodes.
/// </summary>
/// <remarks>
/// This entity is a database representation of media metadata. It gets enhanced with additional properties
/// in the application layer based on XML metadata files populated by metadata agents.
/// Do not use this entity in client-facing APIs; instead, use dedicated DTOs or GraphQL types.
/// </remarks>
public class MetadataItem : SoftDeletableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this metadata item.
    /// </summary>
    public Guid Uuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the type of this metadata item.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the title of this metadata item.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sort title used for alphabetical ordering.
    /// </summary>
    public string SortTitle { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original title of this metadata item.
    /// </summary>
    public string? OriginalTitle { get; set; }

    /// <summary>
    /// Gets or sets the summary or description of this metadata item.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the tagline or slogan for this metadata item.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// Gets or sets the content rating for this metadata item.
    /// </summary>
    public string? ContentRating { get; set; }

    /// <summary>
    /// Gets or sets the age rating associated with the content rating.
    /// </summary>
    public int? ContentRatingAge { get; set; }

    /// <summary>
    /// Gets or sets the release date of this metadata item.
    /// </summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the year of release for this metadata item.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the index number of this metadata item within its parent collection.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Gets or sets the absolute index number of this metadata item across all parent collections.
    /// </summary>
    public int? AbsoluteIndex { get; set; }

    /// <summary>
    /// Gets or sets the duration of this metadata item in seconds.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the URI of the thumbnail image for this metadata item.
    /// </summary>
    public string? ThumbUri { get; set; }

    /// <summary>
    /// Gets or sets the URI of the artwork image for this metadata item.
    /// </summary>
    public string? ArtUri { get; set; }

    /// <summary>
    /// Gets or sets the URI of the logo image for this metadata item.
    /// </summary>
    public string? LogoUri { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the library this metadata item belongs to.
    /// </summary>
    public int LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library that this metadata item belongs to.
    /// </summary>
    public LibrarySection LibrarySection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the parent metadata item.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the parent metadata item for this item.
    /// </summary>
    public MetadataItem? Parent { get; set; }

    /// <summary>
    /// Gets or sets the collection of child metadata items.
    /// </summary>
    public ICollection<MetadataItem> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of media items associated with this metadata item.
    /// </summary>
    public ICollection<MediaItem> MediaItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of user-specific settings associated with this metadata item.
    /// </summary>
    public ICollection<MetadataItemSetting> Settings { get; set; } = [];
}
