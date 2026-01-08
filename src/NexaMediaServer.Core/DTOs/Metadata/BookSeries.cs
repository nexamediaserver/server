// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for book series entries.
/// </summary>
/// <remarks>
/// A book series groups related books together, such as a manga series,
/// a periodical, or a comic book series.
/// </remarks>
public sealed record class BookSeries : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
