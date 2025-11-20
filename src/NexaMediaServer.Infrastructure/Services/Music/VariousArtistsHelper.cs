// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;

namespace NexaMediaServer.Infrastructure.Services.Music;

/// <summary>
/// Provides utilities for detecting and handling "Various Artists" album artist designations.
/// </summary>
/// <remarks>
/// <para>
/// "Various Artists" is a special designation used for compilation albums where tracks
/// are performed by different artists. The album artist is "Various Artists" but individual
/// tracks link to their respective performing artists.
/// </para>
/// <para>
/// This helper provides:
/// <list type="bullet">
///   <item><description>Detection of Various Artists variants in multiple formats</description></item>
///   <item><description>A well-known UUID for the global Various Artists entity</description></item>
///   <item><description>Constants for consistent naming across the application</description></item>
/// </list>
/// </para>
/// </remarks>
public static class VariousArtistsHelper
{
    /// <summary>
    /// The canonical title for the Various Artists group entity.
    /// </summary>
    public const string CanonicalTitle = "Various Artists";

    /// <summary>
    /// Well-known UUID for the global Various Artists group entity.
    /// This UUID is deterministic to allow consistent identification across libraries.
    /// </summary>
    /// <remarks>
    /// Generated from UUID v5 namespace DNS with "various-artists.nexa.ms".
    /// </remarks>
    public static readonly Guid WellKnownUuid = new("89ad4ac3-39f5-5066-8e51-a6a5f49b1f1a");

    /// <summary>
    /// Common English variants used to identify Various Artists designations.
    /// </summary>
    /// <remarks>
    /// These variants are compared case-insensitively and with trimmed whitespace.
    /// </remarks>
    private static readonly FrozenSet<string> VariousArtistsVariants = new[]
    {
        "Various Artists",
        "Various",
        "VA",
        "V/A",
        "V.A.",
        "V. A.",
        "Compilation",
        "Soundtrack",
        "OST",
        "Original Soundtrack",
        "Various Performers",
        "Mixed Artists",
        "Multiple Artists",
        "Diverse Artists",
        "Assorted Artists",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the given artist name represents a Various Artists designation.
    /// </summary>
    /// <param name="artistName">The artist name to check.</param>
    /// <returns>
    /// <c>true</c> if the name is a known Various Artists variant; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsVariousArtists(string? artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return false;
        }

        return VariousArtistsVariants.Contains(artistName.Trim());
    }

    /// <summary>
    /// Normalizes a Various Artists variant to the canonical title.
    /// </summary>
    /// <param name="artistName">The artist name to normalize.</param>
    /// <returns>
    /// The canonical "Various Artists" title if the input is a known variant;
    /// otherwise, the original value trimmed.
    /// </returns>
    public static string? NormalizeArtistName(string? artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return null;
        }

        var trimmed = artistName.Trim();
        return IsVariousArtists(trimmed) ? CanonicalTitle : trimmed;
    }
}
