// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service providing access to section locations.
/// </summary>
public class SectionLocationService : ISectionLocationService
{
    private readonly ISectionLocationRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionLocationService"/> class.
    /// </summary>
    /// <param name="repository">The library folder repository.</param>
    public SectionLocationService(ISectionLocationRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public IQueryable<SectionLocation> GetQueryable()
    {
        return this.repository.GetQueryable();
    }
}
