// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Immutable snapshot of an extra classification result.
/// </summary>
/// <param name="Category">The classified extra category.</param>
/// <param name="MovieFolder">Absolute path to the candidate movie folder.</param>
/// <param name="Title">Normalized human-friendly title for the extra.</param>
internal readonly record struct ExtrasResolverClassification(
    ExtrasResolverExtraCategory Category,
    string MovieFolder,
    string Title
);
