// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for managing library entities.
/// </summary>
public class LibrarySectionRepository : ILibrarySectionRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrarySectionRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public LibrarySectionRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<LibrarySection> GetQueryable()
    {
        return this.context.LibrarySections.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<LibrarySection?> GetByIdAsync(int id)
    {
        return this.context.LibrarySections.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <inheritdoc />
    public Task AddAsync(LibrarySection librarySection)
    {
        this.context.LibrarySections.Add(librarySection);

        return this.context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task<LibrarySection> UpdateAsync(LibrarySection librarySection)
    {
        this.context.LibrarySections.Update(librarySection);
        return this.context.SaveChangesAsync().ContinueWith(t => librarySection);
    }

    /// <inheritdoc />
    public Task<LibrarySection?> GetByIdWithFoldersAsync(int librarySectionId)
    {
        return this
            .context.LibrarySections.Include(l => l.Locations)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == librarySectionId);
    }

    /// <inheritdoc />
    public Task<LibrarySection?> GetByUuidAsync(Guid uuid)
    {
        return this.context.LibrarySections.AsNoTracking().FirstOrDefaultAsync(l => l.Uuid == uuid);
    }
}
