// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides methods for managing media libraries.
/// </summary>
public interface ILibrarySectionService
{
    /// <summary>
    /// Gets a queryable collection of libraries.
    /// </summary>
    /// <returns>An IQueryable of Library objects.</returns>
    IQueryable<LibrarySection> GetQueryable();

    /// <summary>
    /// Gets a library section by its UUID.
    /// </summary>
    /// <param name="uuid">The unique identifier of the library section.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the library section if found; otherwise, null.</returns>
    Task<LibrarySection?> GetByUuidAsync(Guid uuid);

    /// <summary>
    /// Gets the folders associated with a specific library.
    /// </summary>
    /// <param name="libraryId">The ID of the library.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of LibraryFolder objects.</returns>
    Task<List<SectionLocation>> GetLibraryFoldersAsync(int libraryId);

    /// <summary>
    /// Adds a library section and starts scanning it.
    /// </summary>
    /// <param name="library">The library section to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created library section and the queued scan identifier.</returns>
    Task<LibrarySectionCreationResult> AddLibraryAndScanAsync(LibrarySection library);
}
