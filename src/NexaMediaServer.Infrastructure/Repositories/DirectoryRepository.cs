// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for accessing directories within libraries.
/// </summary>
public class DirectoryRepository : IDirectoryRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public DirectoryRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<NexaMediaServer.Core.Entities.Directory> GetQueryable()
    {
        return this.context.Directories.AsNoTracking();
    }

    /// <inheritdoc />
    public Task<NexaMediaServer.Core.Entities.Directory?> GetByIdAsync(int id)
    {
        return this.context.Directories.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <inheritdoc />
    public async Task BulkInsertAsync(
        IEnumerable<NexaMediaServer.Core.Entities.Directory> directories,
        CancellationToken cancellationToken = default
    )
    {
        var list = (
            directories ?? Enumerable.Empty<NexaMediaServer.Core.Entities.Directory>()
        ).ToList();
        if (list.Count == 0)
        {
            return;
        }

        // Apply audit timestamps before bulk insert (BulkExtensions bypasses EF change tracker).
        BulkAuditTimestamps.ApplyInsertTimestamps(list);

        var cfg = new BulkConfig { PreserveInsertOrder = true, SetOutputIdentity = true };

        await this.context.BulkInsertAsync(list, cfg, cancellationToken: cancellationToken);
    }
}
