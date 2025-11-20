// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// Represents a metadata item within a hub.
/// </summary>
/// <param name="Id">The UUID of the metadata item.</param>
/// <param name="Title">The display title.</param>
/// <param name="Year">The release year.</param>
/// <param name="ThumbUri">The thumbnail URI.</param>
/// <param name="MetadataType">The type of metadata.</param>
/// <param name="Duration">The duration in milliseconds (for video/audio).</param>
/// <param name="ViewOffset">The current view offset for resume (user-specific).</param>
/// <param name="LibrarySectionId">The UUID of the library section containing this item.</param>
/// <param name="Tagline">The tagline for hero display.</param>
/// <param name="ArtUri">The backdrop art URI for hero display.</param>
/// <param name="ArtHash">The backdrop art blurhash.</param>
/// <param name="LogoUri">The logo URI for hero display.</param>
/// <param name="LogoHash">The logo blurhash.</param>
/// <param name="ContentRating">The content rating (e.g., PG-13, TV-MA).</param>
/// <param name="Summary">The item summary/description.</param>
public sealed record HubItem(
    Guid Id,
    string Title,
    int? Year,
    string? ThumbUri,
    MetadataType MetadataType,
    int? Duration,
    int ViewOffset,
    Guid LibrarySectionId,
    string? Tagline = null,
    string? ArtUri = null,
    string? ArtHash = null,
    string? LogoUri = null,
    string? LogoHash = null,
    string? ContentRating = null,
    string? Summary = null
);
