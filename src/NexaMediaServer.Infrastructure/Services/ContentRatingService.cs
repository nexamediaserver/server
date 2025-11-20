// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service that resolves content rating strings to normalized age values using hardcoded lookup tables.
/// </summary>
public partial class ContentRatingService : IContentRatingService
{
    /// <summary>
    /// Movie rating lookups grouped by country code.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, int>> MovieRatings = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // United States (MPA)
        ["US"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["PG"] = 8,
            ["PG-13"] = 13,
            ["R"] = 17,
            ["NC-17"] = 18,
        },

        // United Kingdom (BBFC)
        ["UK"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["UC"] = 0,
            ["U"] = 0,
            ["PG"] = 8,
            ["12A"] = 12,
            ["12"] = 12,
            ["15"] = 15,
            ["18"] = 18,
            ["R18"] = 18,
        },

        // Germany (FSK)
        ["DE"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["FSK0"] = 0,
            ["FSK6"] = 6,
            ["FSK12"] = 12,
            ["FSK16"] = 16,
            ["FSK18"] = 18,
            ["0"] = 0,
            ["6"] = 6,
            ["12"] = 12,
            ["16"] = 16,
            ["18"] = 18,
        },

        // France
        ["FR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["TP"] = 0,
            ["-12"] = 12,
            ["12"] = 12,
            ["-16"] = 16,
            ["16"] = 16,
            ["-18"] = 18,
            ["18"] = 18,
        },

        // Japan (Eirin)
        ["JP"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["PG12"] = 12,
            ["R15+"] = 15,
            ["R18+"] = 18,
        },

        // Canada (English)
        ["CA"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["PG"] = 8,
            ["14A"] = 14,
            ["18A"] = 18,
            ["R"] = 18,
        },

        // Canada (Quebec)
        ["CA-QC"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["13+"] = 13,
            ["16+"] = 16,
            ["18+"] = 18,
        },

        // Australia (ACB)
        ["AU"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["PG"] = 8,
            ["M"] = 15,
            ["MA15+"] = 15,
            ["R18+"] = 18,
            ["X18+"] = 18,
        },

        // Brazil (ClassInd)
        ["BR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["L"] = 0,
            ["10"] = 10,
            ["12"] = 12,
            ["14"] = 14,
            ["16"] = 16,
            ["18"] = 18,
        },

        // South Korea (KMRB)
        ["KR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALL"] = 0,
            ["12"] = 12,
            ["15"] = 15,
            ["19"] = 19,
        },

        // Russia
        ["RU"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["0+"] = 0,
            ["6+"] = 6,
            ["12+"] = 12,
            ["16+"] = 16,
            ["18+"] = 18,
        },
    };

    /// <summary>
    /// TV rating lookups grouped by country code.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, int>> TvRatings = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // United States (TV Parental Guidelines)
        ["US"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["TV-Y"] = 0,
            ["TV-Y7"] = 7,
            ["TV-G"] = 0,
            ["TV-PG"] = 8,
            ["TV-14"] = 14,
            ["TV-MA"] = 17,
        },

        // United Kingdom (BBFC - same as movies)
        ["UK"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["UC"] = 0,
            ["U"] = 0,
            ["PG"] = 8,
            ["12"] = 12,
            ["15"] = 15,
            ["18"] = 18,
        },

        // Germany (TV ratings with time restrictions)
        ["DE"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = 0,
            ["6"] = 6,
            ["12"] = 12,
            ["16"] = 16,
            ["18"] = 18,
        },

        // France (TV ratings)
        ["FR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["TP"] = 0,
            ["-10"] = 10,
            ["10"] = 10,
            ["-12"] = 12,
            ["12"] = 12,
            ["-16"] = 16,
            ["16"] = 16,
            ["-18"] = 18,
            ["18"] = 18,
        },

        // Canada (English)
        ["CA"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = 0,
            ["C8"] = 8,
            ["G"] = 0,
            ["PG"] = 8,
            ["14+"] = 14,
            ["18+"] = 18,
        },

        // Canada (Quebec)
        ["CA-QC"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["8"] = 8,
            ["13"] = 13,
            ["16"] = 16,
            ["18"] = 18,
        },

        // Australia (same as movies)
        ["AU"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["G"] = 0,
            ["PG"] = 8,
            ["M"] = 15,
            ["MA15+"] = 15,
            ["AV15+"] = 15,
            ["R18+"] = 18,
        },

        // Brazil (ClassInd - same as movies)
        ["BR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["L"] = 0,
            ["AL"] = 0,
            ["10"] = 10,
            ["A10"] = 10,
            ["12"] = 12,
            ["A12"] = 12,
            ["14"] = 14,
            ["A14"] = 14,
            ["16"] = 16,
            ["A16"] = 16,
            ["18"] = 18,
            ["A18"] = 18,
        },

        // South Korea (same as movies)
        ["KR"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["ALL"] = 0,
            ["7"] = 7,
            ["12"] = 12,
            ["15"] = 15,
            ["19"] = 19,
        },

        // Russia (same as movies)
        ["RU"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["0+"] = 0,
            ["6+"] = 6,
            ["12+"] = 12,
            ["16+"] = 16,
            ["18+"] = 18,
        },
    };

    /// <summary>
    /// Common fallback patterns for ratings without country codes.
    /// Prioritizes US and UK rating patterns.
    /// </summary>
    private static readonly Dictionary<string, int> FallbackRatings = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // US Movie patterns
        ["G"] = 0,
        ["PG"] = 8,
        ["PG-13"] = 13,
        ["R"] = 17,
        ["NC-17"] = 18,

        // US TV patterns
        ["TV-Y"] = 0,
        ["TV-Y7"] = 7,
        ["TV-G"] = 0,
        ["TV-PG"] = 8,
        ["TV-14"] = 14,
        ["TV-MA"] = 17,

        // UK patterns
        ["UC"] = 0,
        ["U"] = 0,
        ["12A"] = 12,
        ["12"] = 12,
        ["15"] = 15,
        ["18"] = 18,

        // Common age-based patterns
        ["0"] = 0,
        ["6"] = 6,
        ["7"] = 7,
        ["8"] = 8,
        ["10"] = 10,
        ["13"] = 13,
        ["14"] = 14,
        ["16"] = 16,
        ["17"] = 17,
        ["19"] = 19,

        // Common suffixed patterns
        ["0+"] = 0,
        ["6+"] = 6,
        ["12+"] = 12,
        ["13+"] = 13,
        ["14+"] = 14,
        ["15+"] = 15,
        ["16+"] = 16,
        ["18+"] = 18,

        // Japan patterns
        ["R15+"] = 15,
        ["R18+"] = 18,

        // Australia/Brazil patterns
        ["M"] = 15,
        ["MA15+"] = 15,
        ["L"] = 0,
        ["AL"] = 0,
    };

    private readonly ILogger<ContentRatingService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentRatingService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ContentRatingService(ILogger<ContentRatingService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public int? ResolveAge(string rating, string? countryCode = null, bool isTelevision = false)
    {
        if (string.IsNullOrWhiteSpace(rating))
        {
            return null;
        }

        // Normalize inputs
        var normalizedRating = rating.Trim().ToUpperInvariant();
        var normalizedCountry = countryCode?.Trim().ToUpperInvariant();

        // Select appropriate lookup table
        var lookupTable = isTelevision ? TvRatings : MovieRatings;

        // Try exact match with country code
        if (!string.IsNullOrEmpty(normalizedCountry))
        {
            if (
                lookupTable.TryGetValue(normalizedCountry, out var countryRatings)
                && countryRatings.TryGetValue(normalizedRating, out var exactAge)
            )
            {
                this.LogExactMatch(normalizedCountry, normalizedRating, exactAge, isTelevision);
                return exactAge;
            }

            this.LogCountryNotMatched(normalizedCountry, normalizedRating, isTelevision);
        }

        // Fallback to common patterns
        if (FallbackRatings.TryGetValue(normalizedRating, out var fallbackAge))
        {
            this.LogFallbackMatch(normalizedRating, fallbackAge, normalizedCountry);
            return fallbackAge;
        }

        // No match found
        this.LogNoMatch(normalizedRating, normalizedCountry, isTelevision);
        return null;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Exact match for content rating: {Country}:{Rating} -> {Age} (TV: {IsTelevision})"
    )]
    private partial void LogExactMatch(string country, string rating, int age, bool isTelevision);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Country-specific match not found for {Country}:{Rating} (TV: {IsTelevision}), attempting fallback"
    )]
    private partial void LogCountryNotMatched(string country, string rating, bool isTelevision);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Fallback match for content rating: {Rating} -> {Age} (Country: {Country})"
    )]
    private partial void LogFallbackMatch(string rating, int age, string? country);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "No match found for content rating: {Rating} (Country: {Country}, TV: {IsTelevision})"
    )]
    private partial void LogNoMatch(string rating, string? country, bool isTelevision);
}
