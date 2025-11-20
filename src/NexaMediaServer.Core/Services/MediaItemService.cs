// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service providing access to media items.
/// </summary>
public class MediaItemService : IMediaItemService
{
    private readonly IMediaItemRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaItemService"/> class.
    /// </summary>
    /// <param name="repository">The media item repository.</param>
    public MediaItemService(IMediaItemRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public IQueryable<MediaItem> GetQueryable()
    {
        return this.repository.GetQueryable();
    }
}
