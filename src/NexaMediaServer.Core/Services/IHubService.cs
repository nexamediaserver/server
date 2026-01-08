// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for retrieving hub definitions and content.
/// </summary>
public interface IHubService
{
    /// <summary>
    /// Gets hub definitions for the home page based on the user's accessible libraries.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of hub definitions for the home page.</returns>
    Task<IReadOnlyList<HubDefinition>> GetHomeHubDefinitionsAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets hub definitions for a library section's discover page.
    /// </summary>
    /// <param name="librarySectionId">The UUID of the library section.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of hub definitions for the library discover page.</returns>
    Task<IReadOnlyList<HubDefinition>> GetLibraryDiscoverHubDefinitionsAsync(
        Guid librarySectionId,
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets hub definitions for a metadata item's detail page.
    /// </summary>
    /// <param name="metadataItemId">The UUID of the metadata item.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of hub definitions for the item detail page.</returns>
    Task<IReadOnlyList<HubDefinition>> GetItemDetailHubDefinitionsAsync(
        Guid metadataItemId,
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the content (items) for a specific hub.
    /// </summary>
    /// <param name="hubType">The type of hub.</param>
    /// <param name="context">The hub context.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="librarySectionId">Optional library section ID for library-specific hubs.</param>
    /// <param name="metadataItemId">Optional metadata item ID for detail page hubs.</param>
    /// <param name="filterValue">Optional filter value for filtered hubs (e.g., genre name).</param>
    /// <param name="count">The maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of hub items.</returns>
    Task<IReadOnlyList<HubItem>> GetHubItemsAsync(
        HubType hubType,
        HubContext context,
        string userId,
        Guid? librarySectionId = null,
        Guid? metadataItemId = null,
        string? filterValue = null,
        int count = 20,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the people (cast/crew) for a specific hub.
    /// </summary>
    /// <param name="hubType">The type of hub (Cast or Crew).</param>
    /// <param name="metadataItemId">The UUID of the metadata item.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="count">The maximum number of people to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of hub people.</returns>
    Task<IReadOnlyList<HubPerson>> GetHubPeopleAsync(
        HubType hubType,
        Guid metadataItemId,
        string userId,
        int count = 20,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a stored hub configuration for a context and optional scope.
    /// </summary>
    /// <param name="context">The hub context.</param>
    /// <param name="librarySectionId">Optional library section UUID for scoped configurations.</param>
    /// <param name="metadataType">Optional metadata type (for item detail hubs).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored configuration if present; otherwise null.</returns>
    Task<HubConfiguration?> GetHubConfigurationAsync(
        HubContext context,
        Guid? librarySectionId,
        MetadataType? metadataType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the hub configuration for the home page.
    /// </summary>
    /// <param name="configuration">The new hub configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<HubConfiguration> UpdateHomeHubConfigurationAsync(
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the hub configuration for a library section's discover page.
    /// </summary>
    /// <param name="librarySectionId">The UUID of the library section.</param>
    /// <param name="configuration">The new hub configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<HubConfiguration> UpdateLibraryHubConfigurationAsync(
        Guid librarySectionId,
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the hub configuration for a specific metadata type and optional library on detail pages.
    /// </summary>
    /// <param name="metadataType">The metadata type the configuration applies to.</param>
    /// <param name="librarySectionId">Optional library section UUID to scope the configuration.</param>
    /// <param name="configuration">The new hub configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<HubConfiguration> UpdateItemDetailHubConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        HubConfiguration configuration,
        CancellationToken cancellationToken = default
    );
}
