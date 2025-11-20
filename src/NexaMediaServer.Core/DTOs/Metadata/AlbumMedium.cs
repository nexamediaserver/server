// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO for album medium entries (disc within an album release).
/// </summary>
/// <remarks>
/// An album medium represents a single disc or other medium within an album release.
/// This maps to MusicBrainz's "Medium" concept. Every album release has at least one
/// medium, even for single-disc releases.
/// </remarks>
public sealed record class AlbumMedium : MetadataBaseItem;

#pragma warning restore S2094 // Classes should not be empty
