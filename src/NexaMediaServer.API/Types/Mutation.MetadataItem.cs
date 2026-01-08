// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

using CoreGenre = NexaMediaServer.Core.Entities.Genre;
using CoreTag = NexaMediaServer.Core.Entities.Tag;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL mutation operations for editing metadata items.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates a metadata item with the specified fields, respecting locked field settings.
    /// </summary>
    /// <param name="input">The update input containing the item ID and fields to update.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success and the updated item.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<UpdateMetadataItemPayload> UpdateMetadataItemAsync(
        UpdateMetadataItemInput input,
        [Service] MediaServerContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            return new UpdateMetadataItemPayload(false, Error: "Input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            return new UpdateMetadataItemPayload(false, Error: "Item ID is required.");
        }

        var item = await dbContext.MetadataItems
            .Include(m => m.Genres)
            .Include(m => m.Tags)
            .Include(m => m.LibrarySection)
            .Include(m => m.ExternalIdentifiers)
            .FirstOrDefaultAsync(m => m.Uuid == input.ItemId, cancellationToken);

        if (item is null)
        {
            return new UpdateMetadataItemPayload(false, Error: $"Metadata item with ID '{input.ItemId}' not found.");
        }

        var lockedFields = new HashSet<string>(item.LockedFields, StringComparer.OrdinalIgnoreCase);

        // Update basic text fields
        if (input.Title is not null && !lockedFields.Contains(MetadataFieldNames.Title))
        {
            item.Title = input.Title;
            item.SortTitle = SortName.Generate(input.Title, "en");
        }

        if (input.SortTitle is not null && !lockedFields.Contains(MetadataFieldNames.SortTitle))
        {
            item.SortTitle = input.SortTitle;
        }

        if (input.OriginalTitle is not null && !lockedFields.Contains(MetadataFieldNames.OriginalTitle))
        {
            item.OriginalTitle = input.OriginalTitle;
        }

        if (input.Summary is not null && !lockedFields.Contains(MetadataFieldNames.Summary))
        {
            item.Summary = input.Summary;
        }

        if (input.Tagline is not null && !lockedFields.Contains(MetadataFieldNames.Tagline))
        {
            item.Tagline = input.Tagline;
        }

        if (input.ContentRating is not null && !lockedFields.Contains(MetadataFieldNames.ContentRating))
        {
            item.ContentRating = input.ContentRating;
        }

        // Update date/numeric fields
        if (input.ReleaseDate.HasValue && !lockedFields.Contains(MetadataFieldNames.ReleaseDate))
        {
            item.ReleaseDate = input.ReleaseDate.Value;
            item.Year = input.ReleaseDate.Value.Year;
        }

        // Update genres if provided
        if (input.Genres is not null && !lockedFields.Contains(MetadataFieldNames.Genres))
        {
            // Clear existing genres and add new ones
            item.Genres.Clear();

            foreach (var genreName in input.Genres.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(genreName))
                {
                    continue;
                }

                var trimmedName = genreName.Trim();

                // Find existing genre or create new one
                var genre = await dbContext.Genres
                    .FirstOrDefaultAsync(g => g.Name == trimmedName, cancellationToken);

                if (genre is null)
                {
                    genre = new CoreGenre { Name = trimmedName };
                    dbContext.Genres.Add(genre);
                }

                item.Genres.Add(genre);
            }
        }

        // Update tags if provided
        if (input.Tags is not null && !lockedFields.Contains(MetadataFieldNames.Tags))
        {
            // Clear existing tags and add new ones
            item.Tags.Clear();

            foreach (var tagName in input.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    continue;
                }

                var trimmedName = tagName.Trim();

                // Find existing tag or create new one
                var tag = await dbContext.Tags
                    .FirstOrDefaultAsync(t => t.Name == trimmedName, cancellationToken);

                if (tag is null)
                {
                    tag = new CoreTag { Name = trimmedName };
                    dbContext.Tags.Add(tag);
                }

                item.Tags.Add(tag);
            }
        }

        // Update external identifiers if provided
        if (input.ExternalIds is not null && !lockedFields.Contains(MetadataFieldNames.ExternalIdentifiers))
        {
            // Remove identifiers not in the input
            var inputProviders = input.ExternalIds
                .Select(e => e.Provider)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toRemove = item.ExternalIdentifiers
                .Where(e => !inputProviders.Contains(e.Provider))
                .ToList();

            foreach (var identifier in toRemove)
            {
                item.ExternalIdentifiers.Remove(identifier);
            }

            // Update or add identifiers
            foreach (var externalIdInput in input.ExternalIds)
            {
                if (string.IsNullOrWhiteSpace(externalIdInput.Provider))
                {
                    continue;
                }

                var existing = item.ExternalIdentifiers
                    .FirstOrDefault(e => string.Equals(e.Provider, externalIdInput.Provider, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    existing.Value = externalIdInput.Value ?? string.Empty;
                }
                else
                {
                    item.ExternalIdentifiers.Add(new ExternalIdentifier
                    {
                        Provider = externalIdInput.Provider,
                        Value = externalIdInput.Value ?? string.Empty,
                        MetadataItemId = item.Id,
                    });
                }
            }
        }

        // Update locked fields if provided
        if (input.LockedFields is not null)
        {
            item.LockedFields = input.LockedFields
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Update extra fields if provided
        if (input.ExtraFields is not null)
        {
            foreach (var extraField in input.ExtraFields)
            {
                if (string.IsNullOrWhiteSpace(extraField.Key))
                {
                    continue;
                }

                item.ExtraFields[extraField.Key] = extraField.Value;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Return the updated item mapped to API type
        var updatedItem = new MetadataItem
        {
            Id = item.Uuid,
            DatabaseId = item.Id,
            ParentDatabaseId = item.ParentId,
            MetadataType = item.MetadataType,
            Title = item.Title,
            TitleSort = string.IsNullOrEmpty(item.SortTitle) ? item.Title : item.SortTitle,
            OriginalTitle = item.OriginalTitle ?? string.Empty,
            Summary = item.Summary ?? string.Empty,
            Tagline = item.Tagline ?? string.Empty,
            ContentRating = item.ContentRating ?? string.Empty,
            Year = item.Year ?? 0,
            OriginallyAvailableAt = item.ReleaseDate,
            ParentId = item.Parent?.Uuid,
            LibrarySectionUuid = item.LibrarySection?.Uuid ?? Guid.Empty,
            Index = item.Index ?? 0,
            Length = item.Duration ?? 0,
            ThumbUri = item.ThumbUri ?? string.Empty,
            ThumbHash = item.ThumbHash ?? string.Empty,
            ArtUri = item.ArtUri ?? string.Empty,
            ArtHash = item.ArtHash ?? string.Empty,
            LogoUri = item.LogoUri ?? string.Empty,
            LogoHash = item.LogoHash ?? string.Empty,
            LeafCount = item.Children.Sum(c => c.Children.Count > 0 ? c.Children.Count : 1),
            ChildCount = item.Children.Count,
            Genres = item.Genres.Select(g => g.Name).ToList(),
            Tags = item.Tags.Select(t => t.Name).ToList(),
            IsPromoted = item.IsPromoted,
            LockedFields = item.LockedFields.ToList(),
            ExtraFieldsRaw = item.ExtraFields,
        };

        return new UpdateMetadataItemPayload(true, Item: updatedItem);
    }
}
