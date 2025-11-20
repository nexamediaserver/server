// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Represents a relation that should be materialized once both metadata items exist in the database.
/// </summary>
/// <param name="RelationType">Type of the relation that should be created.</param>
/// <param name="OwnerMediaPath">Normalized absolute path to the owning metadata's primary media (file or disc directory).</param>
public readonly record struct PendingMetadataRelation(
    RelationType RelationType,
    string OwnerMediaPath
);
