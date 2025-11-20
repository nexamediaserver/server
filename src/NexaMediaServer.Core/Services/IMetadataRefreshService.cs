// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Schedules metadata-only refresh operations for library items.
/// </summary>
public interface IMetadataRefreshService
{
    /// <summary>
    /// Enqueues metadata refresh jobs for a specific item and, optionally, all of its descendants.
    /// </summary>
    /// <param name="itemId">The metadata item UUID.</param>
    /// <param name="includeDescendants">Whether to recurse through the full descendant tree.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enqueue result.</returns>
    Task<MetadataRefreshResult> EnqueueItemRefreshAsync(
        Guid itemId,
        bool includeDescendants,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Enqueues metadata refresh jobs for every item in a library section.
    /// </summary>
    /// <param name="librarySectionId">The library section UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enqueue result.</returns>
    Task<MetadataRefreshResult> EnqueueLibraryRefreshAsync(
        Guid librarySectionId,
        CancellationToken cancellationToken = default
    );
}
