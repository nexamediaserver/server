// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides full-text search capabilities for metadata items.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Gets the expected schema version for the current implementation.
    /// </summary>
    int ExpectedSchemaVersion { get; }

    /// <summary>
    /// Searches the index for metadata items matching the specified query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="pivot">The type of content to filter by. Defaults to <see cref="SearchPivot.Top"/>.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of search results ordered by relevance.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        SearchPivot pivot = SearchPivot.Top,
        int limit = 25,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Indexes or updates one or more metadata items in the search index.
    /// </summary>
    /// <param name="items">The metadata items to index, including their related entities for denormalization.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexItemsAsync(
        IEnumerable<MetadataItem> items,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes a metadata item from the search index.
    /// </summary>
    /// <param name="uuid">The unique identifier of the metadata item to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveFromIndexAsync(Guid uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the entire search index from scratch.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current schema version of the search index.
    /// </summary>
    /// <returns>The schema version, or null if no index exists.</returns>
    int? GetIndexSchemaVersion();
}
