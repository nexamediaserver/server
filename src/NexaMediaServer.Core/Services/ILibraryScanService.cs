// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides query access to library scans.
/// </summary>
public interface ILibraryScanService
{
    /// <summary>
    /// Get queryable for library scans.
    /// </summary>
    /// <returns>An IQueryable of LibraryScan entities.</returns>
    IQueryable<LibraryScan> GetQueryable();
}
