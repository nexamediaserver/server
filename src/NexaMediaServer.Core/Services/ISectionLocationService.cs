// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides query access to section locations.
/// </summary>
public interface ISectionLocationService
{
    /// <summary>
    /// Get queryable for section locations.
    /// </summary>
    /// <returns>An IQueryable of SectionLocation entities.</returns>
    IQueryable<SectionLocation> GetQueryable();
}
