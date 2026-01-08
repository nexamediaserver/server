// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for photo album entries.
/// </summary>
/// <remarks>
/// A photo album groups related photos together, typically by date, event, or location.
/// </remarks>
public sealed record class PhotoAlbum : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
