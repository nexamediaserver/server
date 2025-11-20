// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a batch of coalesced filesystem change events ready for processing.
/// </summary>
public sealed class CoalescedChangeEvent
{
    /// <summary>
    /// Gets the library section ID for these changes.
    /// </summary>
    public required int LibrarySectionId { get; init; }

    /// <summary>
    /// Gets the paths that should be scanned (created or modified).
    /// </summary>
    public required IReadOnlyList<string> PathsToScan { get; init; }

    /// <summary>
    /// Gets the paths that should be removed from the database.
    /// </summary>
    public required IReadOnlyList<string> PathsToRemove { get; init; }

    /// <summary>
    /// Gets the timestamp when the batch was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
