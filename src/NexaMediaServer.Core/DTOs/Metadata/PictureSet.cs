// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for picture set entries.
/// </summary>
/// <remarks>
/// A picture set groups related pictures together, such as a wallpaper collection or art series.
/// </remarks>
public sealed record class PictureSet : MetadataCollectionItem;

#pragma warning restore S2094 // Classes should not be empty
