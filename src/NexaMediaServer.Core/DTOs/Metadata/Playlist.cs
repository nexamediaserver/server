// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for playlist entries.
/// </summary>
/// <remarks>
/// A playlist is an ordered collection of playable items, typically audio tracks or videos.
/// </remarks>
public sealed record class Playlist : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
