// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository interface for accessing media items.
/// </summary>
public interface IMediaItemRepository
{
    /// <summary>
    /// Get queryable for media items.
    /// </summary>
    /// <returns>An IQueryable of MediaItem entities.</returns>
    IQueryable<MediaItem> GetQueryable();

    /// <summary>
    /// Get queryable for media items with tracking enabled (for updates).
    /// </summary>
    /// <returns>A tracking IQueryable of MediaItem entities.</returns>
    IQueryable<MediaItem> GetTrackedQueryable();

    /// <summary>
    /// Get a media item by id.
    /// </summary>
    /// <param name="id">The media item identifier.</param>
    /// <returns>The media item if found; otherwise null.</returns>
    Task<MediaItem?> GetByIdAsync(int id);

    /// <summary>
    /// Update a media item (full entity update).
    /// </summary>
    /// <param name="item">The media item to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(MediaItem item);
}
