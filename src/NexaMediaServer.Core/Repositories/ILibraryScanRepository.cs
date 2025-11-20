// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository interface for managing library scan operations.
/// </summary>
public interface ILibraryScanRepository
{
    /// <summary>
    /// Gets a queryable collection of libraries scans.
    /// </summary>
    /// <returns>An IQueryable of Library scan entities.</returns>
    IQueryable<LibraryScan> GetQueryable();

    /// <summary>
    /// Gets a library scan by its identifier.
    /// </summary>
    /// <param name="id">The scan identifier.</param>
    /// <returns>The library scan if found; otherwise, null.</returns>
    Task<LibraryScan?> GetByIdAsync(int id);

    /// <summary>
    /// Gets the currently active scan for a library.
    /// </summary>
    /// <param name="libraryId">The library identifier.</param>
    /// <returns>The active library scan if found; otherwise, null.</returns>
    Task<LibraryScan?> GetActiveScanAsync(int libraryId);

    /// <summary>
    /// Gets library scans for a specific library.
    /// </summary>
    /// <param name="libraryId">The library identifier.</param>
    /// <param name="limit">The maximum number of scans to return.</param>
    /// <returns>A list of library scans.</returns>
    Task<List<LibraryScan>> GetByLibraryIdAsync(int libraryId, int limit = 10);

    /// <summary>
    /// Adds a new library scan.
    /// </summary>
    /// <param name="scan">The library scan to add.</param>
    /// <returns>The added library scan.</returns>
    Task<LibraryScan> AddAsync(LibraryScan scan);

    /// <summary>
    /// Updates an existing library scan.
    /// </summary>
    /// <param name="scan">The library scan to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(LibraryScan scan);
}
