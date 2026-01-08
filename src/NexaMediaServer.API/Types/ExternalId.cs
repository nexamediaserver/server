// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents an external identifier from a metadata provider.
/// </summary>
/// <param name="Provider">The provider name (e.g., "tmdb", "imdb", "tvdb").</param>
/// <param name="Value">The identifier value from the provider.</param>
public record ExternalId(
    string Provider,
    string Value
);
