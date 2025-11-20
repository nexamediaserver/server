// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for managing library scan entities.
/// </summary>
public class LibraryScanRepository : ILibraryScanRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryScanRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public LibraryScanRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<LibraryScan> GetQueryable()
    {
        return this.context.Set<LibraryScan>().AsNoTracking();
    }

    /// <inheritdoc />
    public Task<LibraryScan?> GetByIdAsync(int id)
    {
        return this.context.Set<LibraryScan>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public Task<LibraryScan?> GetActiveScanAsync(int libraryId)
    {
        return this
            .context.Set<LibraryScan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.LibrarySectionId == libraryId
                && (s.Status == LibraryScanStatus.Pending || s.Status == LibraryScanStatus.Running)
            );
    }

    /// <inheritdoc />
    public Task<List<LibraryScan>> GetByLibraryIdAsync(int libraryId, int limit = 10)
    {
        return this
            .context.Set<LibraryScan>()
            .Where(s => s.LibrarySectionId == libraryId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<LibraryScan> AddAsync(LibraryScan scan)
    {
        this.context.Set<LibraryScan>().Add(scan);
        await this.context.SaveChangesAsync();
        return scan;
    }

    /// <inheritdoc />
    public Task UpdateAsync(LibraryScan scan)
    {
        this.context.Set<LibraryScan>().Update(scan);
        return this.context.SaveChangesAsync();
    }
}
