// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;
using NexaMediaServer.Common;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Services;
using CoreEntity = NexaMediaServer.Core.Entities;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL mutation operations for the API.
/// </summary>
[MutationType]
public static partial class Mutation
{
    /// <summary>
    /// Adds a new library section and schedules an initial scan.
    /// </summary>
    /// <param name="input">The user-provided library section details.</param>
    /// <param name="librarySectionService">The library section service.</param>
    /// <returns>The created library section and scan metadata.</returns>
    /// <exception cref="GraphQLException">Thrown when validation fails.</exception>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<AddLibrarySectionPayload> AddLibrarySectionAsync(
        AddLibrarySectionInput input,
        [Service] ILibrarySectionService librarySectionService
    )
    {
        if (input is null)
        {
            throw CreateGraphQLInputError("Library section input is required.");
        }

        string name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw CreateGraphQLInputError("Library section name is required.");
        }

        // We derive a normalized sort name from the display name.
        // Uses English only for now.
        string sortName = SortName.Generate(name, "en");

        List<string> rootPaths = (input.RootPaths ?? new List<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rootPaths.Count == 0)
        {
            throw CreateGraphQLInputError("At least one root path must be provided.");
        }

        var librarySection = new CoreEntity.LibrarySection
        {
            Name = name,
            SortName = sortName,
            Type = input.Type,
            Locations = rootPaths
                .Select(path => new CoreEntity.SectionLocation
                {
                    RootPath = path,
                    Available = true,
                    LastScannedAt = DateTime.UnixEpoch,
                })
                .ToList(),
            Settings = input.Settings is null
                ? new CoreEntity.LibrarySectionSetting()
                : new CoreEntity.LibrarySectionSetting
                {
                    PreferredMetadataLanguage = input.Settings.PreferredMetadataLanguage,
                    MetadataAgentOrder = input.Settings.MetadataAgentOrder.ToList(),
                    HideSeasonsForSingleSeasonSeries = input
                        .Settings
                        .HideSeasonsForSingleSeasonSeries,
                    EpisodeSortOrder = input.Settings.EpisodeSortOrder,
                    PreferredAudioLanguages = input.Settings.PreferredAudioLanguages.ToList(),
                    PreferredSubtitleLanguages = input.Settings.PreferredSubtitleLanguages.ToList(),
                    MetadataAgentSettings = input.Settings.MetadataAgentSettings.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.ToDictionary(inner => inner.Key, inner => inner.Value)
                    ),
                },
        };

        LibrarySectionCreationResult result = await librarySectionService.AddLibraryAndScanAsync(
            librarySection
        );

        return new AddLibrarySectionPayload(result.LibrarySection, result.ScanId);
    }

    private static GraphQLException CreateGraphQLInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage(message)
                .SetCode("LIBRARY_SECTION_VALIDATION_ERROR")
                .Build()
        );
    }
}
