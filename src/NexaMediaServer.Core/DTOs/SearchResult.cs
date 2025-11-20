// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a single result from a search query.
/// </summary>
/// <param name="Uuid">The unique identifier of the metadata item.</param>
/// <param name="Title">The title of the metadata item.</param>
/// <param name="MetadataType">The type of the metadata item.</param>
/// <param name="Score">The relevance score of the result.</param>
/// <param name="Year">The release year of the metadata item, if available.</param>
/// <param name="ThumbUri">The URI of the thumbnail image, if available.</param>
/// <param name="LibrarySectionId">The library section ID of the metadata item.</param>
public record SearchResult(
    Guid Uuid,
    string Title,
    MetadataType MetadataType,
    float Score,
    int? Year,
    string? ThumbUri,
    Guid LibrarySectionId
);
