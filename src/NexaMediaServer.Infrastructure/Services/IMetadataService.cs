// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// High-level metadata service that combines database entities with on-disk/enriched metadata
/// and maps them to API types. Designed to keep IQueryable provider-side execution for
/// paging/filtering/sorting where possible.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Returns a queryable of raw <see cref="NexaMediaServer.Core.Entities.MetadataItem"/> entities for further mapping.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> of metadata entities.</returns>
    IQueryable<MetadataItem> GetQueryable();

    /// <summary>
    /// Returns a queryable of root <see cref="NexaMediaServer.Core.Entities.MetadataItem"/> entries for a library section.
    /// </summary>
    /// <param name="librarySectionId">Library section UUID.</param>
    /// <returns>An <see cref="IQueryable{T}"/> filtered to root items for the specified section.</returns>
    IQueryable<MetadataItem> GetLibraryRootsQueryable(Guid librarySectionId);

    /// <summary>
    /// Gets a single raw <see cref="NexaMediaServer.Core.Entities.MetadataItem"/> by UUID.
    /// </summary>
    /// <param name="id">Item UUID.</param>
    /// <returns>The entity or <see langword="null"/> when not found.</returns>
    Task<MetadataItem?> GetByUuidAsync(Guid id);

    /// <summary>
    /// Gets extras that are related to the supplied metadata items.
    /// </summary>
    /// <param name="ownerMetadataIds">Database identifiers of potential owners.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A mapping from owner database id to its extras.</returns>
    Task<IReadOnlyDictionary<int, IReadOnlyList<MetadataItem>>> GetExtrasByOwnersAsync(
        IReadOnlyCollection<int> ownerMetadataIds,
        CancellationToken cancellationToken = default
    );
}
