// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Base metadata DTO for collection types that group related items together.
/// </summary>
/// <remarks>
/// Collection types include photo albums, picture sets, book series, game series,
/// game franchises, user collections, and playlists. These types share common
/// behavior for thumbnail generation (2x2 collage from children) and grouping semantics.
/// </remarks>
public record class MetadataCollectionItem : MetadataBaseItem;

#pragma warning restore S2094 // Classes should not be empty
