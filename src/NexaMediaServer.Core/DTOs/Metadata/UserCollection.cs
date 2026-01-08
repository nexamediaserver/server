// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty
#pragma warning disable CA1711 // UserCollection is a domain term, not a collection type

/// <summary>
/// Metadata DTO for user-defined collection entries.
/// </summary>
/// <remarks>
/// A collection is a user-curated grouping of items, such as a movie collection
/// (e.g., "Marvel Cinematic Universe", "Star Wars Saga").
/// </remarks>
public sealed record class UserCollection : MetadataCollectionItem;

#pragma warning restore CA1711 // UserCollection is a domain term, not a collection type
#pragma warning restore S2094 // Classes should not be empty
