// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository interface for accessing metadata items.
/// </summary>
public interface IMetadataItemRepository
{
    /// <summary>
    /// Gets a queryable collection of metadata items.
    /// </summary>
    /// <returns>An IQueryable of MetadataItem entities.</returns>
    IQueryable<MetadataItem> GetQueryable();

    /// <summary>
    /// Gets a tracking queryable collection of metadata items.
    /// </summary>
    /// <returns>An IQueryable of MetadataItem entities with tracking enabled.</returns>
    IQueryable<MetadataItem> GetTrackedQueryable();

    /// <summary>
    /// Gets a metadata item by its UUID.
    /// </summary>
    /// <param name="id">The UUID of the metadata item.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata item.</returns>
    Task<MetadataItem?> GetByUuidAsync(Guid id);

    /// <summary>
    /// Adds a metadata item to the repository.
    /// </summary>
    /// <param name="item">The metadata item to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(MetadataItem item);

    /// <summary>
    /// Performs a bulk insert of metadata items with their related entities for maximum throughput.
    /// </summary>
    /// <param name="items">The metadata items to insert.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BulkInsertAsync(
        IEnumerable<MetadataBaseItem> items,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Persists modifications to an existing tracked metadata item.
    /// </summary>
    /// <param name="item">The modified metadata item (must be tracked or have a valid key).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(MetadataItem item);

    /// <summary>
    /// Retrieves extras related to the provided metadata owners via relation mappings.
    /// </summary>
    /// <param name="ownerMetadataIds">Database identifiers of potential owners.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A mapping from owner id to a list of extras.</returns>
    Task<IReadOnlyDictionary<int, IReadOnlyList<MetadataItem>>> GetExtrasByOwnersAsync(
        IReadOnlyCollection<int> ownerMetadataIds,
        CancellationToken cancellationToken = default
    );
}
