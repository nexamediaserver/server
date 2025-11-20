// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Provides methods for accessing and querying library data.
/// </summary>
public interface ILibrarySectionRepository
{
    /// <summary>
    /// Gets a queryable collection of libraries.
    /// </summary>
    /// <returns>An IQueryable of Library entities.</returns>
    IQueryable<LibrarySection> GetQueryable();

    /// <summary>
    /// Gets a library by its identifier.
    /// </summary>
    /// <param name="id">The library identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library if found; otherwise, null.</returns>
    Task<LibrarySection?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a library section by its UUID.
    /// </summary>
    /// <param name="uuid">The library section UUID.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library section if found; otherwise, null.</returns>
    Task<LibrarySection?> GetByUuidAsync(Guid uuid);

    /// <summary>
    /// Adds a new library section.
    /// </summary>
    /// <param name="librarySection">The library section entity to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(LibrarySection librarySection);

    /// <summary>
    /// Updates an existing library.
    /// </summary>
    /// <param name="librarySection">The library section entity to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<LibrarySection> UpdateAsync(LibrarySection librarySection);

    /// <summary>
    /// Gets a library by its identifier, including its associated folders.
    /// </summary>
    /// <param name="librarySectionId">The library identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library with its folders if found; otherwise, null.</returns>
    Task<LibrarySection?> GetByIdWithFoldersAsync(int librarySectionId);

    /// <summary>
    /// Deletes a library section by its UUID.
    /// </summary>
    /// <param name="uuid">The library section UUID.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid uuid);
}
