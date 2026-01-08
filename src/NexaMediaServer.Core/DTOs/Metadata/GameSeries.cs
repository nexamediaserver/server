// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for video game series entries.
/// </summary>
/// <remarks>
/// A game series is a set of video games that are directly related to each other,
/// typically sharing the same title, characters, and storyline, and are usually released
/// sequentially as part of a continuing narrative or gameplay experience.
/// </remarks>
public sealed record class GameSeries : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
