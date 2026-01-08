// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for video game franchise entries.
/// </summary>
/// <remarks>
/// A game franchise is a collection of multiple related video games, which can be spread
/// across different platforms and genres, but share common elements such as characters,
/// settings, or storylines. Various game series can belong to the same franchise.
/// </remarks>
public sealed record class GameFranchise : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
