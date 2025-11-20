// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents a single result from a search query.
/// </summary>
[GraphQLName("SearchResult")]
public class SearchResult
{
    /// <summary>
    /// Gets the unique identifier of the metadata item.
    /// </summary>
    [ID("Item")]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the title of the metadata item.
    /// </summary>
    public string Title { get; init; } = null!;

    /// <summary>
    /// Gets the type of the metadata item.
    /// </summary>
    public MetadataType MetadataType { get; init; }

    /// <summary>
    /// Gets the relevance score of the search result.
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Gets the release year of the metadata item, if available.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Gets the thumbnail URL of the metadata item, if available.
    /// </summary>
    public string? ThumbUri { get; init; }

    /// <summary>
    /// Gets the library section ID of the metadata item.
    /// </summary>
    [ID("LibrarySection")]
    [GraphQLName("librarySectionId")]
    public Guid LibrarySectionId { get; init; }
}
