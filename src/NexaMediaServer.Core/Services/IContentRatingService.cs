// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service that resolves content rating strings to normalized age values.
/// </summary>
public interface IContentRatingService
{
    /// <summary>
    /// Resolves a content rating to its equivalent minimum age.
    /// </summary>
    /// <param name="rating">The content rating identifier (e.g., "PG-13", "R", "TV-MA").</param>
    /// <param name="countryCode">Optional ISO 3166-1 alpha-2 country code (e.g., "US", "UK", "JP"). Used for exact matching; falls back to common patterns if null or unknown.</param>
    /// <param name="isTelevision">Whether the content is television/streaming (true) or movie (false). Affects which rating system to use.</param>
    /// <returns>The minimum age for the rating, or null if the rating is not recognized.</returns>
    int? ResolveAge(string rating, string? countryCode = null, bool isTelevision = false);
}
