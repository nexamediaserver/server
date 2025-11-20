// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides query access to media items.
/// </summary>
public interface IMediaItemService
{
    /// <summary>
    /// Get queryable for media items.
    /// </summary>
    /// <returns>An IQueryable of MediaItem entities.</returns>
    IQueryable<MediaItem> GetQueryable();
}
