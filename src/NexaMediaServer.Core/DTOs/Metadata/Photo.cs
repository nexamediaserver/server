// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for photo entries.
/// </summary>
/// <remarks>
/// A photo is a representation of real-world imagery, typically captured using a camera or similar device.
/// Photos are typically organized into date and location-based albums.
/// </remarks>
public sealed record class Photo : Image;

#pragma warning restore S2094 // Classes should not be empty
