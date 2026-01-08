// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types.Fields;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for updating a metadata item.
/// </summary>
/// <param name="ItemId">The UUID of the metadata item to update.</param>
/// <param name="Title">The new title for the item.</param>
/// <param name="SortTitle">The new sort title for the item.</param>
/// <param name="OriginalTitle">The new original title for the item.</param>
/// <param name="Summary">The new summary/description for the item.</param>
/// <param name="Tagline">The new tagline for the item.</param>
/// <param name="ContentRating">The new content rating for the item.</param>
/// <param name="ReleaseDate">The new release date for the item.</param>
/// <param name="Genres">The new genres for the item.</param>
/// <param name="Tags">The new tags for the item.</param>
/// <param name="ExternalIds">The external identifiers for the item.</param>
/// <param name="LockedFields">The field names that should be locked from automatic updates.</param>
/// <param name="ExtraFields">The custom extra fields for the item.</param>
public record UpdateMetadataItemInput(
    [property: ID("Item")]
    Guid ItemId,
    string? Title = null,
    string? SortTitle = null,
    string? OriginalTitle = null,
    string? Summary = null,
    string? Tagline = null,
    string? ContentRating = null,
    DateOnly? ReleaseDate = null,
    IReadOnlyList<string>? Genres = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<ExternalIdInput>? ExternalIds = null,
    IReadOnlyList<string>? LockedFields = null,
    IReadOnlyList<ExtraFieldInput>? ExtraFields = null
);
