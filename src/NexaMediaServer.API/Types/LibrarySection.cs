// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Data;

using CoreEntities = NexaMediaServer.Core.Entities;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Representation of a library section for GraphQL queries.
/// </summary>
[Node]
[GraphQLName("LibrarySection")]
public sealed class LibrarySection
{
    /// <summary>
    /// Gets the global Relay-compatible identifier of the library section.
    /// </summary>
    [ID]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the display name of the library section.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the sortable name of the library section.
    /// </summary>
    public string SortName { get; init; } = null!;

    /// <summary>
    /// Gets the type of the library section.
    /// </summary>
    public LibraryType Type { get; init; }

    /// <summary>
    /// Gets the list of root locations for the library section.
    /// </summary>
    public List<string> Locations { get; init; } = null!;

    /// <summary>
    /// Gets the settings for this library section.
    /// </summary>
    public LibrarySectionSettings Settings { get; init; } = null!;

    /// <summary>
    /// Asynchronously retrieves a library section by its identifier.
    /// Placeholder used for Relay Node resolution.
    /// </summary>
    /// <param name="id">The identifier of the library section to retrieve.</param>
    /// <param name="dataLoader">The library section dataloader.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library section.</returns>
    public static async Task<LibrarySection> GetAsync(
        Guid id,
        ILibrarySectionByIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var section = await dataLoader.LoadAsync(id, cancellationToken);
        if (section == null)
        {
            var error = ErrorBuilder
                .New()
                .SetMessage("Library section not found.")
                .SetCode("LIBRARY_SECTION_NOT_FOUND")
                .Build();
            throw new GraphQLException(error);
        }

        return section;
    }

    /// <summary>
    /// Gets the available root item types for browsing this library section.
    /// </summary>
    /// <remarks>
    /// The available types depend on the library type. For example, a Music library
    /// supports browsing by Albums or Artists, while a Movies library only has Movies.
    /// If only one option is available, the UI should hide the type selector.
    /// </remarks>
    /// <returns>A list of browsable item type options.</returns>
    [GraphQLName("availableRootItemTypes")]
    public List<BrowsableItemType> GetAvailableRootItemTypes()
    {
        return BrowseOptionsProvider
            .GetAvailableRootItemTypes(this.Type)
            .Select(opt => new BrowsableItemType
            {
                DisplayName = opt.DisplayName,
                MetadataTypes = opt.MetadataTypes.ToList(),
            })
            .ToList();
    }

    /// <summary>
    /// Gets the available sort fields for browsing this library section.
    /// </summary>
    /// <remarks>
    /// The available sort fields depend on the library type. Some fields require
    /// user-specific data (e.g., Progress, Date Viewed) which may affect performance.
    /// When sorting by fields other than Title, the letter index (jump bar) should be hidden.
    /// </remarks>
    /// <returns>A list of sort field options.</returns>
    [GraphQLName("availableSortFields")]
    public List<SortField> GetAvailableSortFields()
    {
        return BrowseOptionsProvider
            .GetAvailableSortFields(this.Type)
            .Select(opt => new SortField
            {
                Key = opt.Key,
                DisplayName = opt.DisplayName,
                RequiresUserData = opt.RequiresUserDataJoin,
            })
            .ToList();
    }

    /// <summary>
    /// Gets an offset-paginated list of top-level (root) metadata items (those without a parent) for this library section.
    /// Uses skip/take parameters to allow arbitrary position jumping for jump bar navigation.
    /// </summary>
    /// <param name="metadataTypes">Metadata types to include. Items matching any of these types will be returned.</param>
    /// <param name="dataLoader">The batched loader for child metadata.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>An in-memory queryable used by HotChocolate to create a collection segment.</returns>
    [Authorize]
    [GraphQLName("children")]
    [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 100)]
    // Explicit projection handled via mapping expression; UseProjection not required.
    [UseFiltering]
    [UseSorting(Type = typeof(MetadataItemSortType))]
    public async Task<IQueryable<MetadataItem>> ChildrenAsync(
        MetadataType[] metadataTypes,
        IRootMetadataItemsBySectionIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var items =
            await dataLoader.LoadAsync(
                new RootMetadataItemsRequest(this.Id, metadataTypes),
                cancellationToken
            ) ?? Array.Empty<MetadataItem>();
        return items.AsQueryable();
    }

    /// <summary>
    /// Gets the alphabetical index for jump bar navigation.
    /// Returns entries for "#" (non-alphabetic) and A-Z with counts and offsets.
    /// </summary>
    /// <remarks>
    /// This index is only meaningful when items are sorted by title. When sorting by other
    /// fields (year, date added, etc.), the letter index should be hidden in the UI.
    /// </remarks>
    /// <param name="metadataTypes">The metadata types to filter by.</param>
    /// <param name="dataLoader">The batched loader for child metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of letter index entries sorted alphabetically (# first, then A-Z).</returns>
    [Authorize]
    [GraphQLName("letterIndex")]
    public async Task<List<LetterIndexEntry>> GetLetterIndexAsync(
        MetadataType[] metadataTypes,
        IRootMetadataItemsBySectionIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var items =
            await dataLoader.LoadAsync(
                new RootMetadataItemsRequest(this.Id, metadataTypes),
                cancellationToken
            ) ?? Array.Empty<MetadataItem>();

        // Sort items by TitleSort to match pagination order
        var sortedItems = items.OrderBy(i => i.TitleSort).ToList();

        // Group by first character, mapping non-A-Z to "#"
        var letterGroups = new Dictionary<string, (int Count, int FirstOffset)>();
        for (int i = 0; i < sortedItems.Count; i++)
        {
            var item = sortedItems[i];
            var firstChar = string.IsNullOrEmpty(item.TitleSort)
                ? '#'
                : char.ToUpperInvariant(item.TitleSort[0]);

            // Non-alphabetic characters go to "#"
            var letter = char.IsLetter(firstChar) ? firstChar.ToString() : "#";

            if (letterGroups.TryGetValue(letter, out var existing))
            {
                letterGroups[letter] = (existing.Count + 1, existing.FirstOffset);
            }
            else
            {
                letterGroups[letter] = (1, i);
            }
        }

        // Build result with proper ordering: # first, then A-Z
        var result = new List<LetterIndexEntry>();

        if (letterGroups.TryGetValue("#", out var hashEntry))
        {
            result.Add(
                new LetterIndexEntry
                {
                    Letter = "#",
                    Count = hashEntry.Count,
                    FirstItemOffset = hashEntry.FirstOffset,
                }
            );
        }

        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            var letterStr = letter.ToString();
            if (letterGroups.TryGetValue(letterStr, out var entry))
            {
                result.Add(
                    new LetterIndexEntry
                    {
                        Letter = letterStr,
                        Count = entry.Count,
                        FirstItemOffset = entry.FirstOffset,
                    }
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all distinct genres present in this library section.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A flat list of genre names.</returns>
    [Authorize]
    [GraphQLName("genres")]
    public async Task<List<string>> GetGenresAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Genres.Where(g => g.MetadataItems.Any(mi => mi.LibrarySection.Uuid == this.Id))
            .Select(g => g.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all distinct tags present in this library section.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A flat list of tag names.</returns>
    [Authorize]
    [GraphQLName("tags")]
    public async Task<List<string>> GetTagsAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Tags.Where(t => t.MetadataItems.Any(mi => mi.LibrarySection.Uuid == this.Id))
            .Select(t => t.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Maps the core library section settings to the API GraphQL type.
    /// </summary>
    /// <param name="settings">The core settings instance.</param>
    /// <returns>A new API settings instance.</returns>
    internal static LibrarySectionSettings MapSettings(CoreEntities.LibrarySectionSetting? settings)
    {
        if (settings == null)
        {
            return new LibrarySectionSettings();
        }

        return new LibrarySectionSettings
        {
            PreferredMetadataLanguage = settings.PreferredMetadataLanguage,
            MetadataAgentOrder = settings.MetadataAgentOrder.ToList(),
            HideSeasonsForSingleSeasonSeries = settings.HideSeasonsForSingleSeasonSeries,
            EpisodeSortOrder = settings.EpisodeSortOrder,
            PreferredAudioLanguages = settings.PreferredAudioLanguages.ToList(),
            PreferredSubtitleLanguages = settings.PreferredSubtitleLanguages.ToList(),
            MetadataAgentSettings = settings.MetadataAgentSettings.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(inner => inner.Key, inner => inner.Value)
            ),
        };
    }

    /// <summary>
    /// Maps a core library section entity to the GraphQL API type.
    /// </summary>
    /// <param name="section">The core library section entity.</param>
    /// <returns>The API representation.</returns>
    internal static LibrarySection FromEntity(CoreEntities.LibrarySection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new LibrarySection
        {
            Id = section.Uuid,
            Name = section.Name,
            SortName = section.SortName ?? string.Empty,
            Type = section.Type,
            Locations = section.Locations.Select(loc => loc.RootPath).ToList(),
            Settings = MapSettings(section.Settings),
        };
    }
}
