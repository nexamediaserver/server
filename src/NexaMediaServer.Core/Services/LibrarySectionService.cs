// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides services for managing media libraries.
/// </summary>
public class LibrarySectionService : ILibrarySectionService
{
    private readonly ILibraryScannerService libraryScannerService;
    private readonly ILibrarySectionRepository librarySectionRepository;
    private readonly ISectionLocationRepository sectionLocationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrarySectionService"/> class.
    /// </summary>
    /// <param name="libraryScannerService">The library scanner service.</param>
    /// <param name="librarySectionRepository">The library section repository.</param>
    /// <param name="sectionLocationRepository">The section location repository.</param>
    public LibrarySectionService(
        ILibraryScannerService libraryScannerService,
        ILibrarySectionRepository librarySectionRepository,
        ISectionLocationRepository sectionLocationRepository
    )
    {
        this.libraryScannerService = libraryScannerService;
        this.librarySectionRepository = librarySectionRepository;
        this.sectionLocationRepository = sectionLocationRepository;
    }

    /// <inheritdoc/>
    public async Task<LibrarySectionCreationResult> AddLibraryAndScanAsync(LibrarySection library)
    {
        await this.librarySectionRepository.AddAsync(library);
        int scanId = await this.libraryScannerService.StartScanAsync(library.Id);

        return new LibrarySectionCreationResult(library, scanId);
    }

    /// <inheritdoc/>
    public Task<LibrarySection?> GetByUuidAsync(Guid uuid)
    {
        return this.librarySectionRepository.GetByUuidAsync(uuid);
    }

    /// <inheritdoc/>
    public Task<List<SectionLocation>> GetLibraryFoldersAsync(int libraryId)
    {
        return this.sectionLocationRepository.GetByLibraryIdAsync(libraryId);
    }

    /// <inheritdoc/>
    public IQueryable<LibrarySection> GetQueryable()
    {
        return this.librarySectionRepository.GetQueryable();
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveLibrarySectionAsync(Guid uuid)
    {
        var librarySection = await this.librarySectionRepository.GetByUuidAsync(uuid);
        if (librarySection == null)
        {
            return false;
        }

        await this.librarySectionRepository.DeleteAsync(uuid);
        return true;
    }
}
