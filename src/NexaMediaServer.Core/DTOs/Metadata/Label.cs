// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Metadata;

#pragma warning disable S2094 // Classes should not be empty

/// <summary>
/// Metadata DTO representing a label (e.g., record label, movie studio label, book imprint).
/// </summary>
/// <remarks>
/// Labels are organizational subdivisions that release content under a specific brand.
/// A company may own multiple labels, and labels can span different media types
/// (e.g., a publisher's imprint for books, a record label for music releases).
/// </remarks>
public sealed record class Label : MetadataBaseItem;

#pragma warning restore S2094 // Classes should not be empty
