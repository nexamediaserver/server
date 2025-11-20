// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Provides access to library folder data operations.
/// </summary>
public interface ISectionLocationRepository
{
    /// <summary>
    /// Gets a queryable collection of library folders.
    /// </summary>
    /// <returns>An IQueryable of SectionLocation entities.</returns>
    IQueryable<SectionLocation> GetQueryable();

    /// <summary>
    /// Gets a library folder by its identifier.
    /// </summary>
    /// <param name="id">The library folder identifier.</param>
    /// <returns>The library folder if found; otherwise null.</returns>
    Task<SectionLocation?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all library folders for a specific library.
    /// </summary>
    /// <param name="libraryId">The library identifier.</param>
    /// <returns>A list of library folders.</returns>
    Task<List<SectionLocation>> GetByLibraryIdAsync(int libraryId);

    /// <summary>
    /// Checks if a path already exists in any library folder.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="excludeLibraryId">Optional library identifier to exclude from the check.</param>
    /// <returns>True if the path exists; otherwise false.</returns>
    Task<bool> PathExistsAsync(string path, int? excludeLibraryId = null);

    /// <summary>
    /// Adds a new library folder.
    /// </summary>
    /// <param name="folder">The library folder to add.</param>
    /// <returns>The added library folder.</returns>
    Task<SectionLocation> AddAsync(SectionLocation folder);

    /// <summary>
    /// Deletes a library folder by its identifier.
    /// </summary>
    /// <param name="id">The library folder identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(int id);
}
