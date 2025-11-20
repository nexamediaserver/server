// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL query operations for search.
/// </summary>
[QueryType]
public static class SearchQuery
{
    /// <summary>
    /// Searches for metadata items matching the specified query.
    /// </summary>
    /// <param name="searchService">The search service.</param>
    /// <param name="query">The search query string.</param>
    /// <param name="pivot">The type of content to filter by. Defaults to Top (all types).</param>
    /// <param name="limit">The maximum number of results to return. Defaults to 25.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of search results ordered by relevance.</returns>
    [Authorize]
    public static async Task<IReadOnlyList<SearchResult>> Search(
        [Service] ISearchService searchService,
        string query,
        SearchPivot pivot = SearchPivot.Top,
        int limit = 25,
        CancellationToken cancellationToken = default
    )
    {
        // Clamp limit to reasonable bounds
        limit = Math.Clamp(limit, 1, 100);

        var results = await searchService.SearchAsync(query, pivot, limit, cancellationToken);

        return results
            .Select(r => new SearchResult
            {
                Id = r.Uuid,
                Title = r.Title,
                MetadataType = r.MetadataType,
                Score = r.Score,
                Year = r.Year,
                ThumbUri = r.ThumbUri,
                LibrarySectionId = r.LibrarySectionId,
            })
            .ToList();
    }
}
