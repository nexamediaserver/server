// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Represents the outcome of resolving the owning movie for a discovered extra.
/// </summary>
internal enum ExtrasResolverOwnerResolutionStatus
{
    /// <summary>Movie ownership resolved successfully.</summary>
    Success,

    /// <summary>The movie folder no longer exists.</summary>
    MissingMovieFolder,

    /// <summary>No eligible primary video files were discovered in the folder.</summary>
    NoEligibleFiles,

    /// <summary>Multiple candidate movies prevented selecting a single owner.</summary>
    AmbiguousCandidates,
}
