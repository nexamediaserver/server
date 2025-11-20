// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Represents a person cast member linked to a metadata item with optional role text.
/// </summary>
/// <param name="Person">The person metadata entry.</param>
/// <param name="RelationType">The relation type connecting the person to the item.</param>
/// <param name="RelationText">Optional free-form text describing the relationship (e.g., character name).</param>
public sealed record CastMember(
    MetadataItem Person,
    RelationType RelationType,
    string? RelationText
);
