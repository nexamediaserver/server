// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Implementation of genre normalization service.
/// </summary>
public class GenreNormalizationService : IGenreNormalizationService
{
    private readonly GenreNormalizationOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenreNormalizationService"/> class.
    /// </summary>
    /// <param name="options">The genre normalization options.</param>
    public GenreNormalizationService(IOptions<GenreNormalizationOptions> options)
    {
        this.options = options.Value;
    }

    /// <inheritdoc/>
    public string NormalizeGenreName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Check for exact match first
        if (this.options.Mappings.TryGetValue(input, out var normalized))
        {
            return normalized;
        }

        // Check case-insensitive match
        var match = this.options.Mappings.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, input, StringComparison.OrdinalIgnoreCase)
        );

        return !string.IsNullOrEmpty(match.Value) ? match.Value : input;
    }
}
