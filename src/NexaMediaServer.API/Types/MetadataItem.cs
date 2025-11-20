// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Representation of a metadata item for pagination queries.
/// </summary>
[Node]
[GraphQLName("Item")]
public class MetadataItem
{
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
    /// Gets the backdrop URL of the metadata item.
    /// </summary>
    public string? ArtUri { get; init; } = null!;

    /// <summary>
    /// Gets the logo URL of the metadata item.
    /// </summary>
    public string? LogoUri { get; init; } = null!;

    /// <summary>
    /// Gets the theme URL of the metadata item.
    /// </summary>
    public string? ThemeUrl { get; init; } = null!;

    /// <summary>
    /// Gets the parent identifier of the metadata item.
    /// </summary>
    [ID(nameof(MetadataItem))]
    public Guid ParentId { get; init; }

    /// <summary>
    /// Gets the grandparent identifier of the metadata item.
    /// </summary>
    [ID(nameof(MetadataItem))]
    public Guid GrandparentId { get; init; }

    /// <summary>
    /// Gets the parent title of the metadata item.
    /// </summary>
    public string ParentTitle { get; init; } = null!;

    /// <summary>
    /// Gets the parent thumbnail URL of the metadata item.
    /// </summary>
    public Uri ParentThumbnailUrl { get; init; } = null!;

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
    /// Gets the number of viewed leaf items in the metadata item.
    /// </summary>
    public int ViewedLeafCount { get; init; }

    /// <summary>
    /// Gets the number of child items in the metadata item.
    /// </summary>
    public int ChildCount { get; init; }

    /// <summary>
    /// Gets the direct play URL for streaming the first media part of this item.
    /// Returns null if no media parts are available.
    /// </summary>
    public string? DirectPlayUrl { get; init; }

    /// <summary>
    /// Gets the trickplay thumbnail track URL (WebVTT format) for video scrubbing.
    /// Returns null if no trickplay data is available.
    /// </summary>
    public string? TrickplayUrl { get; init; }

    /// <summary>
    /// Gets the number of times the current user has viewed the metadata item.
    /// </summary>
    public int ViewCount { get; init; }

    /// <summary>
    /// Gets the last playback position offset (milliseconds) for the current user.
    /// </summary>
    public int ViewOffset { get; init; }

    /// <summary>
    /// Gets the current user's rating for the metadata item, or 0 if unset.
    /// </summary>
    public float Rating { get; init; }

    /// <summary>
    /// Asynchronously retrieves a metadata item by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the metadata item to retrieve.</param>
    /// <param name="metadataItemService">The metadata item service.</param>
    /// <param name="claimsPrincipal">The current user principal.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata item.</returns>
    public static async Task<MetadataItem> GetAsync(
        Guid id,
        IMetadataItemService metadataItemService,
        ClaimsPrincipal claimsPrincipal
    )
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var item = await metadataItemService
            .GetQueryable()
            .Where(m => m.Uuid == id)
            .Select(MetadataMappings.ToApiTypeForUser(userId))
            .FirstOrDefaultAsync();

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
}
