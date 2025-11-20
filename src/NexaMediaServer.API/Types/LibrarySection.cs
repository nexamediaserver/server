// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using HotChocolate.Authorization;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services;
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
    /// <param name="librarySectionService">The library section service.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library section.</returns>
    public static async Task<LibrarySection> GetAsync(
        Guid id,
        ILibrarySectionService librarySectionService
    )
    {
        var section = await librarySectionService.GetByUuidAsync(id);
        if (section == null)
        {
            var error = ErrorBuilder
                .New()
                .SetMessage("Library section not found.")
                .SetCode("LIBRARY_SECTION_NOT_FOUND")
                .Build();
            throw new GraphQLException(error);
        }

        return new LibrarySection
        {
            Id = id,
            Name = section?.Name ?? string.Empty,
            SortName = section?.SortName ?? string.Empty,
            Type = section!.Type,
            Locations =
                section?.Locations.Select(loc => loc.RootPath).ToList() ?? new List<string>(),
            Settings = MapSettings(section?.Settings),
        };
    }

    /// <summary>
    /// Gets a Relay-paginated list of top-level (root) metadata items (those without a parent) for this library section.
    /// </summary>
    /// <param name="service">The metadata item service.</param>
    /// <param name="claimsPrincipal">The current user principal.</param>
    /// <returns>A queryable used by HotChocolate to create a connection.</returns>
    [Authorize]
    [GraphQLName("children")]
    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100, DefaultPageSize = 100)]
    // Explicit projection handled via mapping expression; UseProjection not required.
    [UseFiltering]
    [UseSorting(typeof(MetadataItemSortType))]
    public IQueryable<MetadataItem> Children(
        [Service] IMetadataService service,
        ClaimsPrincipal claimsPrincipal
    )
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        return service
            .GetLibraryRootsQueryable(this.Id)
            .Select(MetadataMappings.ToApiTypeForUser(userId));
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
}
