// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using DirectoryEntity = NexaMediaServer.Core.Entities.Directory;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository interface for accessing directories within libraries.
/// </summary>
public interface IDirectoryRepository
{
    /// <summary>
    /// Get queryable for directories.
    /// </summary>
    /// <returns>An IQueryable of Directory entities.</returns>
    IQueryable<DirectoryEntity> GetQueryable();

    /// <summary>
    /// Get a directory by id.
    /// </summary>
    /// <param name="id">The directory identifier.</param>
    /// <returns>The directory if found; otherwise null.</returns>
    Task<DirectoryEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Bulk insert a collection of directories. Parent/child ordering should be preserved in the input list.
    /// </summary>
    /// <param name="directories">Directories to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the insert operation has finished.</returns>
    Task BulkInsertAsync(
        IEnumerable<DirectoryEntity> directories,
        CancellationToken cancellationToken = default
    );
}
