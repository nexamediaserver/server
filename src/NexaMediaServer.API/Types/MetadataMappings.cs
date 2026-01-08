// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;

using CoreEntity = NexaMediaServer.Core.Entities.MetadataItem;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Provides reusable projection expressions for metadata types.
/// </summary>
public static class MetadataMappings
{
    /// <summary>
    /// Gets the default projection for metadata items mapped to their API representation.
    /// </summary>
    public static Expression<Func<CoreEntity, MetadataItem>> ToApiType =>
        m => new MetadataItem
        {
            Id = m.Uuid,
            DatabaseId = m.Id,
            ParentDatabaseId = m.ParentId,
            MetadataType = m.MetadataType,
            Title = m.Title,
            TitleSort = string.IsNullOrEmpty(m.SortTitle) ? m.Title : m.SortTitle!,
            OriginalTitle = m.OriginalTitle ?? string.Empty,
            Summary = m.Summary ?? string.Empty,
            Tagline = m.Tagline ?? string.Empty,
            ContentRating = m.ContentRating ?? string.Empty,
            Year = m.Year ?? 0,
            OriginallyAvailableAt = m.ReleaseDate,
            ParentId = m.Parent != null ? m.Parent.Uuid : null,
            LibrarySectionUuid = m.LibrarySection.Uuid,
            Index = m.Index ?? 0,
            Length = (m.Duration ?? 0) * 1000,
            ThumbUri = m.ThumbUri ?? string.Empty,
            ThumbHash = m.ThumbHash ?? string.Empty,
            ArtUri = m.ArtUri ?? string.Empty,
            ArtHash = m.ArtHash ?? string.Empty,
            LogoUri = m.LogoUri ?? string.Empty,
            LogoHash = m.LogoHash ?? string.Empty,
            LeafCount = m.Children.Sum(c => c.Children.Count > 0 ? c.Children.Count : 1),
            ChildCount = m.Children.Count,
            Genres = m.Genres.Select(g => g.Name).ToList(),
            Tags = m.Tags.Select(t => t.Name).ToList(),
            IsPromoted = m.IsPromoted,
            ContentRatingAge = m.ContentRatingAge,
            CreatedAt = m.CreatedAt,
            LockedFields = m.LockedFields.ToList(),
            ExtraFieldsRaw = m.ExtraFields,
        };

    /// <summary>
    /// Legacy helper retained for backward compatibility with existing code paths.
    /// </summary>
    /// <param name="unused">Unused user identifier.</param>
    /// <returns>The default projection.</returns>
    public static Expression<Func<CoreEntity, MetadataItem>> ToApiTypeForUser(
        string? unused = null
    ) => ToApiType;
}
