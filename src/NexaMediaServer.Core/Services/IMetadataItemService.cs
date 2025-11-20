// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for managing metadata items.
/// </summary>
public interface IMetadataItemService
{
    /// <summary>
    /// Get queryable for metadata items.
    /// </summary>
    /// <returns>A queryable collection of metadata items.</returns>
    IQueryable<MetadataItem> GetQueryable();

    /// <summary>
    /// Asynchronously retrieves a metadata item by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the metadata item to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata item.</returns>
    Task<MetadataItem?> GetByUuidAsync(Guid id);

    /// <summary>
    /// Asynchronously retrieves the root metadata items for a given library section.
    /// </summary>
    /// <param name="librarySectionId">The UUID of the library section.</param>
    /// <returns>An IQueryable for the root metadata items of the specified library section.</returns>
    IQueryable<MetadataItem> GetLibraryRootsQueryable(Guid librarySectionId);

    /// <summary>
    /// Apply filters to a metadata item query.
    /// </summary>
    /// <param name="query">The queryable collection of metadata items to filter.</param>
    /// <param name="filters">The filter criteria to apply.</param>
    /// <returns>A filtered queryable collection of metadata items.</returns>
    IQueryable<MetadataItem> ApplyFilters(
        IQueryable<MetadataItem> query,
        MetadataItemFilterInput? filters
    );
}
