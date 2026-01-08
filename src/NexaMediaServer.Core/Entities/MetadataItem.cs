// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

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
    /// Gets or sets the ThumbHash placeholder for the thumbnail image.
    /// </summary>
    public string? ThumbHash { get; set; }

    /// <summary>
    /// Gets or sets the URI of the artwork image for this metadata item.
    /// </summary>
    public string? ArtUri { get; set; }

    /// <summary>
    /// Gets or sets the ThumbHash placeholder for the artwork image.
    /// </summary>
    public string? ArtHash { get; set; }

    /// <summary>
    /// Gets or sets the URI of the logo image for this metadata item.
    /// </summary>
    public string? LogoUri { get; set; }

    /// <summary>
    /// Gets or sets the ThumbHash placeholder for the logo image.
    /// </summary>
    public string? LogoHash { get; set; }

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

    /// <summary>
    /// Gets or sets the set of relations originating from this metadata item.
    /// </summary>
    public ICollection<MetadataRelation> OutgoingRelations { get; set; } = [];

    /// <summary>
    /// Gets or sets the set of relations targeting this metadata item.
    /// </summary>
    public ICollection<MetadataRelation> IncomingRelations { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of genres associated with this metadata item.
    /// </summary>
    /// <remarks>
    /// Only leaf genres are assigned; parent genres in the hierarchy are not automatically included.
    /// </remarks>
    public ICollection<Genre> Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of tags associated with this metadata item.
    /// </summary>
    public ICollection<Tag> Tags { get; set; } = [];

    // ----------------------------- Promotion Properties -----------------------------

    /// <summary>
    /// Gets or sets a value indicating whether this item is promoted to the hero carousel.
    /// </summary>
    /// <remarks>
    /// Only root items (ParentId == null) should be promoted.
    /// </remarks>
    public bool IsPromoted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this item was promoted.
    /// </summary>
    public DateTime? PromotedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when promotion expires.
    /// </summary>
    /// <remarks>
    /// If set, a background job should automatically unpromote the item after this time.
    /// A Hangfire recurring job can be implemented to check PromotedUntil expiration.
    /// </remarks>
    public DateTime? PromotedUntil { get; set; }

    // ----------------------------- External Identifiers -----------------------------

    /// <summary>
    /// Gets or sets the collection of external identifiers from metadata providers.
    /// </summary>
    /// <remarks>
    /// External identifiers link this metadata item to entries in external metadata sources
    /// such as TMDB, TVDB, IMDb, MusicBrainz, etc. Each provider can have at most one
    /// identifier per metadata item.
    /// </remarks>
    public ICollection<ExternalIdentifier> ExternalIdentifiers { get; set; } = [];

    // ----------------------------- Field Locking -----------------------------

    /// <summary>
    /// Gets or sets the collection of field names that are locked from automatic updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a field name is present in this collection, metadata agents and automated refresh
    /// processes will skip updating that field. This allows users to manually set values that
    /// should not be overwritten by metadata providers.
    /// </para>
    /// <para>
    /// Use constants from <c>MetadataFieldNames</c> for built-in fields. Custom/dynamic field
    /// names are also supported for user-defined fields.
    /// </para>
    /// <para>
    /// Locks can be selectively overridden by passing the field names in the <c>overrideFields</c>
    /// parameter when calling refresh methods.
    /// </para>
    /// </remarks>
    public ICollection<string> LockedFields { get; set; } = [];

    // ----------------------------- Extra Fields -----------------------------

    /// <summary>
    /// Gets or sets additional custom fields as a JSON dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property stores admin-defined custom fields with typed values.
    /// Keys correspond to <see cref="Entities.CustomFieldDefinition.Key"/> values.
    /// Values can be strings, numbers, booleans, or arrays serialized as <see cref="JsonElement"/>.
    /// </para>
    /// <para>
    /// Stored as a TEXT column with JSON serialization in the database.
    /// </para>
    /// </remarks>
    public Dictionary<string, JsonElement> ExtraFields { get; set; } = [];
}
