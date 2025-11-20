// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using HotChocolate.Authorization;
using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.Core.Enums;
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
    /// Gets a Relay-paginated list of top-level (root) metadata items (those without a parent) for this library section.
    /// </summary>
    /// <param name="metadataType">Optional metadata type constraint.</param>
    /// <param name="dataLoader">The batched loader for child metadata.</param>
    /// <param name="cancellationToken">Token used to cancel the resolver.</param>
    /// <returns>An in-memory queryable used by HotChocolate to create a connection.</returns>
    [Authorize]
    [GraphQLName("children")]
    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 100)]
    // Explicit projection handled via mapping expression; UseProjection not required.
    [UseFiltering]
    [UseSorting(Type = typeof(MetadataItemSortType))]
    public async Task<IQueryable<MetadataItem>> ChildrenAsync(
        MetadataType metadataType,
        IRootMetadataItemsBySectionIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        var items =
            await dataLoader.LoadAsync(
                new RootMetadataItemsRequest(this.Id, metadataType),
                cancellationToken
            ) ?? Array.Empty<MetadataItem>();
        return items.AsQueryable();
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
