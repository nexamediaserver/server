// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for picture entries.
/// </summary>
/// <remarks>
/// A picture is an image file that may include photographs, illustrations, wallpapers, or other visual representations.
/// It differs from a photo in that it may not necessarily represent real-world imagery and does not fit
/// into the date or location-based album structure typically associated with photos.
/// </remarks>
public sealed record class Picture : Image;

#pragma warning restore S2094 // Classes should not be empty
