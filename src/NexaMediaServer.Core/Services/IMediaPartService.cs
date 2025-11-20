// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides query access to media parts.
/// </summary>
public interface IMediaPartService
{
    /// <summary>
    /// Get queryable for media parts.
    /// </summary>
    /// <returns>An IQueryable of MediaPart entities.</returns>
    IQueryable<MediaPart> GetQueryable();
}
