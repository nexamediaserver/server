// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

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
    /// Gets the parent identifier of the metadata item.
    /// </summary>
    [ID("Item")]
    public Guid ParentId { get; init; }

    /// <summary>
    /// Gets the grandparent identifier of the metadata item.
    /// </summary>
    [ID("Item")]
    public Guid GrandparentId { get; init; }

    /// <summary>
    /// Gets the parent title of the metadata item.
    /// </summary>
    public string ParentTitle { get; init; } = null!;

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
