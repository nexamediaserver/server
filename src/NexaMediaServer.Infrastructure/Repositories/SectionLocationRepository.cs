// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for managing library folder entities.
/// </summary>
public class SectionLocationRepository : ISectionLocationRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionLocationRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public SectionLocationRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<SectionLocation> GetQueryable()
    {
        return this.context.SectionsLocations.AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<SectionLocation> AddAsync(SectionLocation folder)
    {
        folder.CreatedAt = DateTime.UtcNow;

        this.context.SectionsLocations.Add(folder);
        await this.context.SaveChangesAsync();

        return folder;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var folder = await this.context.SectionsLocations.FindAsync(id);
        if (folder != null)
        {
            this.context.SectionsLocations.Remove(folder);
            await this.context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public Task<SectionLocation?> GetByIdAsync(int id)
    {
        return this.context.SectionsLocations.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    }

    /// <inheritdoc />
    public Task<List<SectionLocation>> GetByLibraryIdAsync(int libraryId)
    {
        return this
            .context.SectionsLocations.Where(f => f.LibrarySectionId == libraryId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public Task<bool> PathExistsAsync(string path, int? excludeLibraryId = null)
    {
        throw new NotImplementedException();
    }
}
