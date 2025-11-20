// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for normalizing genre names to canonical forms.
/// </summary>
public interface IGenreNormalizationService
{
    /// <summary>
    /// Normalizes a genre name to its canonical form based on configured mappings.
    /// </summary>
    /// <param name="input">The input genre name.</param>
    /// <returns>The normalized genre name, or the original input if no mapping exists.</returns>
    string NormalizeGenreName(string input);
}
