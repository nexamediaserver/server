// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing media items.
/// </summary>
public class MediaItemRepository : IMediaItemRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaItemRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public MediaItemRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<MediaItem> GetQueryable()
    {
        return this.context.MediaItems.AsNoTracking();
    }

    /// <inheritdoc />
    public IQueryable<MediaItem> GetTrackedQueryable()
    {
        // Default tracking enabled
        return this.context.MediaItems;
    }

    /// <inheritdoc />
    public Task<MediaItem?> GetByIdAsync(int id)
    {
        return this.context.MediaItems.AsNoTracking().FirstOrDefaultAsync(mi => mi.Id == id);
    }

    /// <inheritdoc />
    public Task UpdateAsync(MediaItem item)
    {
        this.context.MediaItems.Update(item);
        return this.context.SaveChangesAsync();
    }
}
