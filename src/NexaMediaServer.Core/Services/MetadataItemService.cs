// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for managing metadata items.
/// </summary>
public class MetadataItemService : IMetadataItemService
{
    private readonly IMetadataItemRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItemService"/> class.
    /// </summary>
    /// <param name="repository">The metadata item repository.</param>
    public MetadataItemService(IMetadataItemRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc/>
    public IQueryable<MetadataItem> GetQueryable()
    {
        return this.repository.GetQueryable();
    }

    /// <inheritdoc/>
    public IQueryable<MetadataItem> ApplyFilters(
        IQueryable<MetadataItem> query,
        MetadataItemFilterInput? filters
    )
    {
        if (filters == null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
        {
            string searchQuery = filters.SearchQuery.Trim().ToLower(CultureInfo.InvariantCulture);
            query = query.Where(item =>
                (item.Title ?? string.Empty).Contains(
                    searchQuery,
                    StringComparison.CurrentCultureIgnoreCase
                )
                || (item.OriginalTitle ?? string.Empty).Contains(
                    searchQuery,
                    StringComparison.CurrentCultureIgnoreCase
                )
            );
        }

        return query;
    }

    /// <inheritdoc/>
    public async Task<MetadataItem?> GetByUuidAsync(Guid id)
    {
        return await this.repository.GetByUuidAsync(id);
    }

    /// <inheritdoc/>
    public IQueryable<MetadataItem> GetLibraryRootsQueryable(Guid librarySectionId)
    {
        // Filter on repository queryable to allow provider-side execution
        return this
            .repository.GetQueryable()
            .Where(mi => mi.LibrarySection.Uuid == librarySectionId && mi.ParentId == null);
    }
}
