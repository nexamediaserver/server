// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Base metadata DTO that mirrors <see cref="MetadataItem"/> minus the <c>MetadataType</c> discriminator.
/// </summary>
public record class MetadataBaseItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the metadata entry.
    /// </summary>
    public Guid Uuid { get; set; }

    /// <summary>
    /// Gets or sets the display title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized sort title.
    /// </summary>
    public string SortTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original release title.
    /// </summary>
    public string? OriginalTitle { get; set; }

    /// <summary>
    /// Gets or sets the synopsis text.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the tagline or slogan.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// Gets or sets the content rating label.
    /// </summary>
    public string? ContentRating { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 country code associated with the content rating (e.g., "US", "UK", "JP").
    /// Used transiently during metadata ingestion to resolve the age rating.
    /// </summary>
    public string? ContentRatingCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the normalized age derived from the content rating.
    /// </summary>
    public int? ContentRatingAge { get; set; }

    /// <summary>
    /// Gets or sets the primary release date.
    /// </summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the index relative to the parent item.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Gets or sets the index relative to the root of the library.
    /// </summary>
    public int? AbsoluteIndex { get; set; }

    /// <summary>
    /// Gets or sets the runtime duration in seconds.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URI.
    /// </summary>
    public string? ThumbUri { get; set; }

    /// <summary>
    /// Gets or sets the ThumbHash placeholder for the thumbnail.
    /// </summary>
    public string? ThumbHash { get; set; }

    /// <summary>
    /// Gets or sets the artwork URI.
    /// </summary>
    public string? ArtUri { get; set; }

    /// <summary>
    /// Gets or sets the ThumbHash placeholder for the backdrop.
    /// </summary>
    public string? ArtHash { get; set; }

    /// <summary>
    /// Gets or sets the logo URI.
    /// </summary>
    public string? LogoUri { get; set; }

    /// <summary>
    /// Gets or sets the ThumbHash placeholder for the logo.
    /// </summary>
    public string? LogoHash { get; set; }

    /// <summary>
    /// Gets or sets the owning library section identifier.
    /// </summary>
    public int LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the owning library section reference.
    /// </summary>
    public LibrarySection? LibrarySection { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent metadata item.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the parent metadata item.
    /// </summary>
    public MetadataBaseItem? Parent { get; set; }

    /// <summary>
    /// Gets or sets the collection of child metadata items.
    /// </summary>
    public ICollection<MetadataBaseItem> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of associated media items.
    /// </summary>
    public ICollection<MediaItem> MediaItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of user settings attached to this metadata item.
    /// </summary>
    public ICollection<MetadataItemSetting> Settings { get; set; } = [];

    /// <summary>
    /// Gets the collection of relations that should be created once both metadata items exist.
    /// </summary>
    public IList<PendingMetadataRelation> PendingRelations { get; } =
        new List<PendingMetadataRelation>();

    /// <summary>
    /// Gets or sets the collection of extra fields for storing type-specific metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dictionary stores arbitrary key-value data that is persisted to
    /// <see cref="MetadataItem.ExtraFields"/>. Keys should use values from
    /// <see cref="Constants.ExtraFieldKeys"/> for consistency.
    /// </para>
    /// <para>
    /// Examples include music-specific fields like release type, media format,
    /// catalog number, and classical music work/movement information.
    /// </para>
    /// </remarks>
    public Dictionary<string, object?> ExtraFields { get; set; } = [];

    /// <summary>
    /// Gets the collection of external identifiers to register for this item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This collection holds external IDs discovered during metadata extraction that should be
    /// persisted as <see cref="Entities.ExternalIdentifier"/> entities. The tuple contains
    /// the provider name (use values from <see cref="Constants.ExternalIdProviders"/>) and the ID value.
    /// </para>
    /// </remarks>
    public IList<(string Provider, string Id)> PendingExternalIds { get; } =
        new List<(string Provider, string Id)>();
}
