// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration options for genre normalization.
/// </summary>
public class GenreNormalizationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "GenreNormalization";

    /// <summary>
    /// Gets or sets the genre name mappings for normalization.
    /// </summary>
    /// <remarks>
    /// Maps input genre names (keys) to canonical genre names (values).
    /// Example: { "Sci-Fi": "Science Fiction", "SciFi": "Science Fiction" }.
    /// </remarks>
    public Dictionary<string, string> Mappings { get; set; } =
        new()
        {
            // Common sci-fi variations
            ["Sci-Fi"] = "Science Fiction",
            ["SciFi"] = "Science Fiction",
            ["Sci Fi"] = "Science Fiction",

            // Documentary variations
            ["Doc"] = "Documentary",
            ["Docu"] = "Documentary",

            // Animation variations
            ["Anime"] = "Animation",
            ["Cartoon"] = "Animation",

            // Music genre normalizations
            ["R&B"] = "Rhythm and Blues",
            ["Hip-Hop"] = "Hip Hop",
            ["Rap"] = "Hip Hop",
        };
}
