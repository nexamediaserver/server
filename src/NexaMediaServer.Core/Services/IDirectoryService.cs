// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using DirectoryEntity = NexaMediaServer.Core.Entities.Directory;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides query access to directories.
/// </summary>
public interface IDirectoryService
{
    /// <summary>
    /// Get queryable for directories.
    /// </summary>
    /// <returns>An IQueryable of Directory entities.</returns>
    IQueryable<DirectoryEntity> GetQueryable();
}
