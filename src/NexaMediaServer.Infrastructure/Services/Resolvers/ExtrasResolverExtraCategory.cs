// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Differentiates between supported local extra categories.
/// </summary>
internal enum ExtrasResolverExtraCategory
{
    /// <summary>Represents a trailer extra.</summary>
    Trailer,

    /// <summary>Represents an ancillary clip (deleted scene, featurette, etc.).</summary>
    Clip,
}
