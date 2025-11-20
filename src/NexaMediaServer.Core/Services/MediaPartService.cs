// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service providing access to media parts.
/// </summary>
public class MediaPartService : IMediaPartService
{
    private readonly IMediaPartRepository repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaPartService"/> class.
    /// </summary>
    /// <param name="repository">The media part repository.</param>
    public MediaPartService(IMediaPartRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public IQueryable<MediaPart> GetQueryable()
    {
        return this.repository.GetQueryable();
    }
}
