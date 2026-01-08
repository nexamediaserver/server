// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Hubs;
using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL query operations for hubs.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Gets hub definitions for the home page (aggregated from user's accessible libraries).
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of hub definitions.</returns>
    [Authorize]
    public static async Task<IEnumerable<HubDefinitionType>> GetHomeHubDefinitionsAsync(
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var definitions = await hubService.GetHomeHubDefinitionsAsync(userId, cancellationToken);
        return definitions.Select(MapToHubDefinitionType);
    }

    /// <summary>
    /// Gets hub definitions for a library's discover page.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of hub definitions.</returns>
    [Authorize]
    public static async Task<IEnumerable<HubDefinitionType>> GetLibraryDiscoverHubDefinitionsAsync(
        [ID] Guid librarySectionId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var definitions = await hubService.GetLibraryDiscoverHubDefinitionsAsync(
            librarySectionId,
            userId,
            cancellationToken
        );
        return definitions.Select(MapToHubDefinitionType);
    }

    /// <summary>
    /// Gets hub definitions for an item's detail page.
    /// </summary>
    /// <param name="itemId">The metadata item ID.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of hub definitions.</returns>
    [Authorize]
    public static async Task<IEnumerable<HubDefinitionType>> GetItemDetailHubDefinitionsAsync(
        [ID] Guid itemId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var definitions = await hubService.GetItemDetailHubDefinitionsAsync(
            itemId,
            userId,
            cancellationToken
        );
        return definitions.Select(MapToHubDefinitionType);
    }

    /// <summary>
    /// Gets hub items for a specific hub type and context.
    /// </summary>
    /// <param name="input">The hub items query input.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of metadata items.</returns>
    [Authorize]
    public static async Task<IEnumerable<MetadataItem>> GetHubItemsAsync(
        GetHubItemsInput input,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var items = await hubService.GetHubItemsAsync(
            input.HubType,
            input.Context,
            userId,
            input.LibrarySectionId,
            input.MetadataItemId,
            input.FilterValue,
            cancellationToken: cancellationToken
        );
        return items.Select(MapToMetadataItem);
    }

    /// <summary>
    /// Gets hub people for a specific hub type.
    /// </summary>
    /// <param name="hubType">The type of hub (Cast or Crew).</param>
    /// <param name="metadataItemId">The metadata item ID.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of metadata items representing people.</returns>
    [Authorize]
    public static async Task<IEnumerable<MetadataItem>> GetHubPeopleAsync(
        HubType hubType,
        [ID] Guid metadataItemId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var people = await hubService.GetHubPeopleAsync(
            hubType,
            metadataItemId,
            userId,
            cancellationToken: cancellationToken
        );
        return people.Select(MapPersonToMetadataItem);
    }

    private static string GetUserId(IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );
        return userId ?? throw new UnauthorizedAccessException("User ID not found in claims.");
    }

    private static HubDefinitionType MapToHubDefinitionType(HubDefinition definition) =>
        new()
        {
            Key =
                $"{definition.HubType}_{definition.Context}_{definition.FilterValue ?? "default"}",
            Type = definition.HubType,
            Title = definition.Title,
            MetadataType = definition.MetadataType,
            Widget = definition.Widget,
            LibrarySectionId = null,
            ContextId = null,
            FilterValue = definition.FilterValue,
        };

    private static MetadataItem MapToMetadataItem(HubItem item) =>
        new()
        {
            Id = item.Id,
            MetadataType = item.MetadataType,
            Title = item.Title,
            TitleSort = string.Empty,
            OriginalTitle = string.Empty,
            Summary = item.Summary ?? string.Empty,
            Tagline = item.Tagline ?? string.Empty,
            ContentRating = item.ContentRating ?? string.Empty,
            Year = item.Year ?? 0,
            OriginallyAvailableAt = null,
            ThumbUri = item.ThumbUri,
            ThumbHash = null,
            ArtUri = item.ArtUri,
            ArtHash = item.ArtHash,
            LogoUri = item.LogoUri,
            LogoHash = item.LogoHash,
            ThemeUrl = null,
            ParentId = item.ParentId,
            LibrarySectionUuid = item.LibrarySectionId,
            Index = item.Index ?? 0,
            Length = item.Duration ?? 0,
            LeafCount = 0,
            ChildCount = 0,
            Context = null,
            DatabaseId = 0,
            ParentDatabaseId = null,
        };

    private static MetadataItem MapPersonToMetadataItem(HubPerson person) =>
        new()
        {
            Id = person.PersonId,
            MetadataType = MetadataType.Person,
            Title = person.Name,
            TitleSort = string.Empty,
            OriginalTitle = string.Empty,
            Summary = string.Empty,
            Tagline = string.Empty,
            ContentRating = string.Empty,
            Year = 0,
            OriginallyAvailableAt = null,
            ThumbUri = person.ThumbUrl,
            ThumbHash = null,
            ArtUri = null,
            ArtHash = null,
            LogoUri = null,
            LogoHash = null,
            ThemeUrl = null,
            ParentId = null,
            LibrarySectionUuid = Guid.Empty,
            Index = 0,
            Length = 0,
            LeafCount = 0,
            ChildCount = 0,
            Context = person.Role,
            DatabaseId = 0,
            ParentDatabaseId = null,
        };
}
