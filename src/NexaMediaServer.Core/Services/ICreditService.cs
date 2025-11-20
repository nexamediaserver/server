// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service responsible for managing person and group credits (cast, crew, artists)
/// associated with metadata items.
/// </summary>
public interface ICreditService
{
    /// <summary>
    /// Upserts person and group credits for a metadata item.
    /// Creates new Person/Group MetadataItem entities as needed and
    /// establishes MetadataRelation records linking them to the owner.
    /// </summary>
    /// <param name="owner">The metadata item that owns these credits.</param>
    /// <param name="people">Person credits to upsert (cast, directors, writers, etc.).</param>
    /// <param name="groups">Group credits to upsert (studios, networks, bands, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if any changes were persisted; otherwise <see langword="false"/>.</returns>
    Task<bool> UpsertCreditsAsync(
        MetadataItem owner,
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        CancellationToken cancellationToken = default
    );
}
