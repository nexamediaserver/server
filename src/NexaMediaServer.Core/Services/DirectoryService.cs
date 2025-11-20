// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Repositories;
using DirectoryEntity = NexaMediaServer.Core.Entities.Directory;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service providing access to directories.
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly IDirectoryRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryService"/> class.
    /// </summary>
    /// <param name="repository">The directory repository.</param>
    public DirectoryService(IDirectoryRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public IQueryable<DirectoryEntity> GetQueryable()
    {
        return this.repository.GetQueryable();
    }
}
