// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Representation of a metadata item for pagination queries.
/// </summary>
[Node]
[GraphQLName("Item")]
public class MetadataItem
{
    /// <summary>
    /// Per-request cache of the current user's metadata settings for this item.
    /// </summary>
    [GraphQLIgnore]
    private MetadataItemSetting? cachedSetting;

    /// <summary>
    /// Gets the global Relay-compatible identifier of the metadata item.
    /// </summary>
    [ID]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the type of the metadata item.
    /// </summary>
    public MetadataType MetadataType { get; init; }

    /// <summary>
    /// Gets the title of the metadata item.
    /// </summary>
    public string Title { get; init; } = null!;

    /// <summary>
    /// Gets the sortable title of the metadata item.
    /// </summary>
    public string TitleSort { get; init; } = null!;

    /// <summary>
    /// Gets the original title of the metadata item.
    /// </summary>
    public string OriginalTitle { get; init; } = null!;

    /// <summary>
    /// Gets the summary description of the metadata item.
    /// </summary>
    public string Summary { get; init; } = null!;

    /// <summary>
    /// Gets the tagline of the metadata item.
    /// </summary>
    public string Tagline { get; init; } = null!;

    /// <summary>
    /// Gets the content rating of the metadata item.
    /// </summary>
    public string ContentRating { get; init; } = null!;

    /// <summary>
    /// Gets the year the metadata item was released.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Gets the date the metadata item was originally available.
    /// </summary>
    public DateOnly? OriginallyAvailableAt { get; init; }

    /// <summary>
    /// Gets the thumbnail URL of the metadata item.
    /// </summary>
    public string? ThumbUri { get; init; } = null!;

    /// <summary>
    /// Gets the ThumbHash placeholder for the thumbnail.
    /// </summary>
    public string? ThumbHash { get; init; } = null!;

    /// <summary>
    /// Gets the backdrop URL of the metadata item.
    /// </summary>
    public string? ArtUri { get; init; } = null!;

    /// <summary>
    /// Gets the ThumbHash placeholder for the backdrop.
    /// </summary>
    public string? ArtHash { get; init; } = null!;

    /// <summary>
    /// Gets the logo URL of the metadata item.
    /// </summary>
    public string? LogoUri { get; init; } = null!;

    /// <summary>
    /// Gets the ThumbHash placeholder for the logo.
    /// </summary>
    public string? LogoHash { get; init; } = null!;

    /// <summary>
    /// Gets the theme URL of the metadata item.
    /// </summary>
    public string? ThemeUrl { get; init; } = null!;

    /// <summary>
    /// Gets the internal parent identifier used for recursive parent resolution.
    /// </summary>
    [GraphQLIgnore]
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets the owning library section identifier (Relay GUID).
    /// </summary>
    [ID("LibrarySection")]
    [GraphQLName("librarySectionId")]
    public Guid LibrarySectionUuid { get; init; }

    /// <summary>
    /// Gets the index of the metadata item.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets the length of the metadata item in milliseconds.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Gets the number of leaf items in the metadata item.
    /// </summary>
    public int LeafCount { get; init; }

    /// <summary>
    /// Gets the list of genres associated with this metadata item.
    /// </summary>
    public List<string> Genres { get; init; } = [];

    /// <summary>
    /// Gets the list of tags associated with this metadata item.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the list of field names that are locked from automatic updates.
    /// </summary>
    /// <remarks>
    /// When a field is locked, metadata agents and automated refresh processes
    /// will not overwrite its value. This allows users to manually set values
    /// that should be preserved.
    /// </remarks>
    public List<string> LockedFields { get; init; } = [];

    /// <summary>
    /// Gets the raw extra fields dictionary. Used internally for EF Core projection.
    /// </summary>
    [GraphQLIgnore]
    public Dictionary<string, JsonElement> ExtraFieldsRaw { get; init; } = [];

    /// <summary>
    /// Gets the number of child items in the metadata item.
    /// </summary>
    public int ChildCount { get; init; }

    /// <summary>
    /// Gets an optional context-specific string (e.g., role name for people in hubs).
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets a value indicating whether this item is promoted (featured in the Promoted hub).
    /// </summary>
    public bool IsPromoted { get; init; }

    /// <summary>
    /// Gets the content rating age value for sorting purposes.
    /// </summary>
    [GraphQLIgnore]
    public int? ContentRatingAge { get; init; }

    /// <summary>
    /// Gets the date and time when this item was added to the library.
    /// </summary>
    [GraphQLIgnore]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the database identifier of the metadata item.
    /// </summary>
    [GraphQLIgnore]
    public int DatabaseId { get; init; }

    /// <summary>
    /// Gets the parent database identifier if available.
    /// </summary>
    [GraphQLIgnore]
    public int? ParentDatabaseId { get; init; }

    /// <summary>
    /// Asynchronously retrieves a metadata item by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the metadata item to retrieve.</param>
    /// <param name="dataLoader">The metadata item data loader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata item.</returns>
    public static async Task<MetadataItem> GetAsync(
        Guid id,
        IMetadataItemByIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var item = await dataLoader.LoadAsync(id, cancellationToken);

        if (item == null)
        {
            var error = ErrorBuilder
                .New()
                .SetMessage($"Metadata item with ID '{id}' not found.")
                .SetCode("METADATA_ITEM_NOT_FOUND")
                .Build();
            throw new GraphQLException(error);
        }

        return item;
    }

    /// <summary>
    /// Gets the extra fields associated with this metadata item.
    /// </summary>
    /// <returns>A list of extra field key-value pairs.</returns>
    [GraphQLName("extraFields")]
    public List<Fields.ExtraFieldType> GetExtraFields() =>
        this.ExtraFieldsRaw
            .Select(kvp => new Fields.ExtraFieldType { Key = kvp.Key, Value = kvp.Value })
            .ToList();

    /// <summary>
    /// Resolves the parent metadata item when available.
    /// </summary>
    /// <param name="dataLoader">The data loader for fetching metadata items by ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parent metadata item or null if none exists.</returns>
    [GraphQLName("parent")]
    public async Task<MetadataItem?> GetParentAsync(
        IMetadataItemByIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        if (!this.ParentId.HasValue || this.ParentId == Guid.Empty)
        {
            return null;
        }

        return await dataLoader.LoadAsync(this.ParentId.Value, cancellationToken);
    }

    /// <summary>
    /// Gets an offset-paginated list of child metadata items for this item.
    /// Useful for PhotoAlbum, PictureSet, Season, and other container types.
    /// </summary>
    /// <param name="dataLoader">The batched loader for child metadata items.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>An in-memory queryable used by HotChocolate to create a collection segment.</returns>
    [Authorize]
    [GraphQLName("children")]
    [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 100)]
    [UseFiltering]
    [UseSorting(Type = typeof(MetadataItemSortType))]
    public async Task<IQueryable<MetadataItem>> GetChildrenAsync(
        IChildMetadataItemsByParentIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var items = await dataLoader.LoadAsync(this.Id, cancellationToken)
            ?? Array.Empty<MetadataItem>();
        return items.AsQueryable();
    }

    /// <summary>
    /// Resolves the number of times the current user has viewed the metadata item.
    /// </summary>
    /// <param name="settingsLoader">The per-user settings dataloader.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The number of completed views for the current user.</returns>
    [GraphQLName("viewCount")]
    public async Task<int> GetViewCountAsync(
        IMetadataItemSettingByUserDataLoader settingsLoader,
        CancellationToken cancellationToken
    )
    {
        var settings = await this.LoadUserSettingsAsync(settingsLoader, cancellationToken);
        return settings?.ViewCount ?? 0;
    }

    /// <summary>
    /// Resolves the resume offset for the current user.
    /// </summary>
    /// <param name="settingsLoader">The per-user settings dataloader.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The playback offset in milliseconds.</returns>
    [GraphQLName("viewOffset")]
    public async Task<int> GetViewOffsetAsync(
        IMetadataItemSettingByUserDataLoader settingsLoader,
        CancellationToken cancellationToken
    )
    {
        var settings = await this.LoadUserSettingsAsync(settingsLoader, cancellationToken);
        return settings?.ViewOffset ?? 0;
    }

    /// <summary>
    /// Resolves the user rating for the metadata item.
    /// </summary>
    /// <param name="settingsLoader">The per-user settings dataloader.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The rating value or 0 when unset.</returns>
    [GraphQLName("rating")]
    public async Task<float> GetRatingAsync(
        IMetadataItemSettingByUserDataLoader settingsLoader,
        CancellationToken cancellationToken
    )
    {
        var settings = await this.LoadUserSettingsAsync(settingsLoader, cancellationToken);
        return settings?.Rating ?? 0;
    }

    /// <summary>
    /// Resolves the primary person or group for a music album.
    /// For albums, this looks at persons linked to child tracks and returns the first one.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The primary person or group, or null if none exists.</returns>
    [GraphQLName("primaryPerson")]
    public async Task<MetadataItem?> GetPrimaryPersonAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken
    )
    {
        // AlbumRelease = 21, AlbumReleaseGroup = 20
        if (this.MetadataType != MetadataType.AlbumRelease &&
            this.MetadataType != MetadataType.AlbumReleaseGroup)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // For albums, get the album's database ID first (might need to look it up by UUID)
        int albumDbId = this.DatabaseId;
        if (albumDbId == 0)
        {
            albumDbId = await context.MetadataItems
                .AsNoTracking()
                .Where(m => m.Uuid == this.Id)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (albumDbId == 0)
            {
                return null;
            }
        }

        // Get all descendant track IDs (albums can have mediums as children, which have tracks)
        // For AlbumReleaseGroup: children are AlbumRelease, grandchildren are AlbumMedium, great-grandchildren are Track
        // For AlbumRelease: children are AlbumMedium, grandchildren are Track
        var descendantTrackIds = await context.MetadataItems
            .AsNoTracking()
            .Where(m => m.MetadataType == MetadataType.Track)
            .Where(m =>
                m.ParentId == albumDbId ||
                m.Parent!.ParentId == albumDbId ||
                m.Parent!.Parent!.ParentId == albumDbId)
            .Select(m => m.Id)
            .Take(100) // Limit to prevent huge queries
            .ToListAsync(cancellationToken);

        if (descendantTrackIds.Count == 0)
        {
            return null;
        }

        // Find the first person/group linked to any of these tracks
        var personId = await context.MetadataRelations
            .AsNoTracking()
            .Where(r => descendantTrackIds.Contains(r.RelatedMetadataItemId))
            .Where(r => r.RelationType == RelationType.PersonContributesToAudio ||
                        r.RelationType == RelationType.GroupContributesToAudio)
            .OrderBy(r => r.Id)
            .Select(r => r.MetadataItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (personId == 0)
        {
            return null;
        }

        // Load the full person/group entity with library section
        var entity = await context.MetadataItems
            .AsNoTracking()
            .Include(m => m.LibrarySection)
            .FirstOrDefaultAsync(m => m.Id == personId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return new MetadataItem
        {
            Id = entity.Uuid,
            MetadataType = entity.MetadataType,
            Title = entity.Title,
            TitleSort = entity.SortTitle,
            OriginalTitle = entity.OriginalTitle ?? string.Empty,
            Summary = entity.Summary ?? string.Empty,
            Tagline = entity.Tagline ?? string.Empty,
            ContentRating = entity.ContentRating ?? string.Empty,
            Year = entity.Year ?? 0,
            OriginallyAvailableAt = entity.ReleaseDate,
            ThumbUri = entity.ThumbUri,
            ThumbHash = entity.ThumbHash,
            ArtUri = entity.ArtUri,
            ArtHash = entity.ArtHash,
            LogoUri = entity.LogoUri,
            LogoHash = entity.LogoHash,
            ParentId = entity.Parent?.Uuid,
            LibrarySectionUuid = entity.LibrarySection?.Uuid ?? Guid.Empty,
            Index = entity.Index ?? 0,
            Length = (entity.Duration ?? 0) * 1000,
            DatabaseId = entity.Id,
            ParentDatabaseId = entity.ParentId,
        };
    }

    /// <summary>
    /// Resolves all persons and groups for a music track.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The list of persons and groups.</returns>
    [GraphQLName("persons")]
    public async Task<List<MetadataItem>> GetPersonsAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken
    )
    {
        // Track = 23 in the enum
        if (this.MetadataType != MetadataType.Track)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Query relations where this track is the target (RelatedMetadataItemId)
        // and the source (MetadataItemId) is a Person or Group contributing to audio
        var relations = await context.MetadataRelations
            .AsNoTracking()
            .Where(r => r.RelatedMetadataItemId == this.DatabaseId ||
                        (this.DatabaseId == 0 && r.RelatedMetadataItem!.Uuid == this.Id))
            .Where(r => r.RelationType == RelationType.PersonContributesToAudio ||
                        r.RelationType == RelationType.GroupContributesToAudio)
            .Select(r => new
            {
                r.MetadataItemId,
                Person = r.MetadataItem,
            })
            .ToListAsync(cancellationToken);

        if (relations.Count == 0)
        {
            return [];
        }

        // Load the full person/group entities with their library sections
        var personIds = relations.Select(r => r.MetadataItemId).Distinct().ToList();
        var persons = await context.MetadataItems
            .AsNoTracking()
            .Include(m => m.LibrarySection)
            .Where(m => personIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        return persons
            .Select(entity => new MetadataItem
            {
                Id = entity.Uuid,
                MetadataType = entity.MetadataType,
                Title = entity.Title,
                TitleSort = entity.SortTitle,
                OriginalTitle = entity.OriginalTitle ?? string.Empty,
                Summary = entity.Summary ?? string.Empty,
                Tagline = entity.Tagline ?? string.Empty,
                ContentRating = entity.ContentRating ?? string.Empty,
                Year = entity.Year ?? 0,
                OriginallyAvailableAt = entity.ReleaseDate,
                ThumbUri = entity.ThumbUri,
                ThumbHash = entity.ThumbHash,
                ArtUri = entity.ArtUri,
                ArtHash = entity.ArtHash,
                LogoUri = entity.LogoUri,
                LogoHash = entity.LogoHash,
                ParentId = entity.Parent?.Uuid,
                LibrarySectionUuid = entity.LibrarySection?.Uuid ?? Guid.Empty,
                Index = entity.Index ?? 0,
                Length = (entity.Duration ?? 0) * 1000,
                DatabaseId = entity.Id,
                ParentDatabaseId = entity.ParentId,
            })
            .ToList();
    }

    /// <summary>
    /// Resolves external identifiers (TMDB, IMDb, TVDB, etc.) for the metadata item.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>A list of external identifiers.</returns>
    [GraphQLName("externalIds")]
    public async Task<List<ExternalId>> GetExternalIdsAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken
    )
    {
        if (this.DatabaseId == 0)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var externalIds = await context.ExternalIdentifiers
            .AsNoTracking()
            .Where(e => e.MetadataItemId == this.DatabaseId)
            .Select(e => new ExternalId(e.Provider, e.Value))
            .ToListAsync(cancellationToken);

        return externalIds;
    }

    /// <summary>
    /// Loads or retrieves from cache the per-user metadata settings for the current item.
    /// </summary>
    /// <param name="settingsLoader">The settings dataloader.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>The current user's metadata settings if available.</returns>
    private async Task<MetadataItemSetting?> LoadUserSettingsAsync(
        IMetadataItemSettingByUserDataLoader settingsLoader,
        CancellationToken cancellationToken
    )
    {
        if (this.DatabaseId == 0)
        {
            return null;
        }

        if (this.cachedSetting != null)
        {
            return this.cachedSetting;
        }

        this.cachedSetting = await settingsLoader.LoadAsync(this.DatabaseId, cancellationToken);
        return this.cachedSetting;
    }
}
