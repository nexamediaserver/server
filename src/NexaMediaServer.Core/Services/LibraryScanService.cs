// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service providing access to library scans.
/// </summary>
public class LibraryScanService : ILibraryScanService
{
    private readonly ILibraryScanRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryScanService"/> class.
    /// </summary>
    /// <param name="repository">The library scan repository.</param>
    public LibraryScanService(ILibraryScanRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public IQueryable<LibraryScan> GetQueryable()
    {
        return this.repository.GetQueryable();
    }
}
