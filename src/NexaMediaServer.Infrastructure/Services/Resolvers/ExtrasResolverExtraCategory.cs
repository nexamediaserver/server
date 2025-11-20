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

    /// <summary>Represents behind-the-scenes material.</summary>
    BehindTheScenes,

    /// <summary>Represents a deleted scene extra.</summary>
    DeletedScene,

    /// <summary>Represents a featurette extra.</summary>
    Featurette,

    /// <summary>Represents an interview extra.</summary>
    Interview,

    /// <summary>Represents a scene extra.</summary>
    Scene,

    /// <summary>Represents a short-form extra.</summary>
    Short,

    /// <summary>Represents an uncategorized extra.</summary>
    Other,
}
