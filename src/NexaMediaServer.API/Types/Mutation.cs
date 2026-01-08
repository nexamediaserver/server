// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services;

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
    /// <param name="sectionLocationRepository">The section location repository.</param>
    /// <returns>The created library section and scan metadata.</returns>
    /// <exception cref="GraphQLException">Thrown when validation fails.</exception>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<AddLibrarySectionPayload> AddLibrarySectionAsync(
        AddLibrarySectionInput input,
        [Service] ILibrarySectionService librarySectionService,
        [Service] ISectionLocationRepository sectionLocationRepository
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

        // Validate that paths don't already exist in other libraries
        foreach (var path in rootPaths)
        {
            if (await sectionLocationRepository.PathExistsAsync(path))
            {
                throw CreateGraphQLInputError(
                    $"The path '{path}' is already assigned to another library or overlaps with an existing library location."
                );
            }
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
                    DisabledMetadataAgents = input.Settings.DisabledMetadataAgents.ToList(),
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

    /// <summary>
    /// Enqueues a metadata-only refresh for a metadata item (optionally its descendants).
    /// </summary>
    /// <param name="input">The item and options to refresh.</param>
    /// <param name="metadataRefreshService">Service used to enqueue refresh jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success or error.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<RefreshMetadataPayload> RefreshItemMetadataAsync(
        RefreshItemMetadataInput input,
        [Service] IMetadataRefreshService metadataRefreshService,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreateMetadataGraphQLInputError("Metadata item input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            throw CreateMetadataGraphQLInputError("Metadata item id is required.");
        }

        var result = await metadataRefreshService.EnqueueItemRefreshAsync(
            input.ItemId,
            input.IncludeChildren,
            cancellationToken
        );

        return result.Success
            ? new RefreshMetadataPayload(true)
            : new RefreshMetadataPayload(false, result.Error);
    }

    /// <summary>
    /// Enqueues metadata-only refresh jobs for an entire library section.
    /// </summary>
    /// <param name="input">The target library section.</param>
    /// <param name="metadataRefreshService">Service used to enqueue refresh jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success or error.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<RefreshMetadataPayload> RefreshLibraryMetadataAsync(
        RefreshLibraryMetadataInput input,
        [Service] IMetadataRefreshService metadataRefreshService,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreateMetadataGraphQLInputError("Library section input is required.");
        }

        if (input.LibrarySectionId == Guid.Empty)
        {
            throw CreateMetadataGraphQLInputError("Library section id is required.");
        }

        var result = await metadataRefreshService.EnqueueLibraryRefreshAsync(
            input.LibrarySectionId,
            cancellationToken
        );

        return result.Success
            ? new RefreshMetadataPayload(true)
            : new RefreshMetadataPayload(false, result.Error);
    }

    /// <summary>
    /// Starts a full filesystem scan for an entire library section.
    /// </summary>
    /// <param name="input">The target library section.</param>
    /// <param name="librarySectionRepository">Repository for retrieving the library section.</param>
    /// <param name="libraryScannerService">Service used to start library scans.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success, scan ID, or error.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<StartLibraryScanPayload> StartLibraryScanAsync(
        StartLibraryScanInput input,
        [Service] ILibrarySectionRepository librarySectionRepository,
        [Service] ILibraryScannerService libraryScannerService,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreateMetadataGraphQLInputError("Library section input is required.");
        }

        if (input.LibrarySectionId == Guid.Empty)
        {
            throw CreateMetadataGraphQLInputError("Library section id is required.");
        }

        var librarySection = await librarySectionRepository.GetByUuidAsync(input.LibrarySectionId);

        if (librarySection is null)
        {
            return new StartLibraryScanPayload(success: false, error: "Library section not found.");
        }

        var scanId = await libraryScannerService.StartScanAsync(librarySection.Id);

        return new StartLibraryScanPayload(success: true, scanId: scanId);
    }

    /// <summary>
    /// Enqueues file analysis, GoP-index generation, and trickplay generation for a metadata item.
    /// </summary>
    /// <param name="input">The item to analyze.</param>
    /// <param name="metadataItemRepository">Repository for verifying item existence.</param>
    /// <param name="fileAnalysisOrchestrator">Service for file analysis jobs.</param>
    /// <param name="imageOrchestrationService">Service for trickplay generation jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success or error.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<AnalyzeItemPayload> AnalyzeItemAsync(
        AnalyzeItemInput input,
        [Service] IMetadataItemRepository metadataItemRepository,
        [Service] IFileAnalysisOrchestrator fileAnalysisOrchestrator,
        [Service] IImageOrchestrationService imageOrchestrationService,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreateMetadataGraphQLInputError("Analyze item input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            throw CreateMetadataGraphQLInputError("Metadata item id is required.");
        }

        var item = await metadataItemRepository.GetByUuidAsync(input.ItemId);
        if (item is null)
        {
            return new AnalyzeItemPayload(false, "Metadata item not found.");
        }

        // Enqueue file analysis job (includes GoP-index generation).
        Hangfire.BackgroundJob.Enqueue<IFileAnalysisOrchestrator>(
            svc => svc.AnalyzeFilesAsync(input.ItemId)
        );

        // Enqueue trickplay generation job.
        Hangfire.BackgroundJob.Enqueue<IImageOrchestrationService>(
            svc => svc.GenerateTrickplayAsync(input.ItemId)
        );

        return new AnalyzeItemPayload(true);
    }

    /// <summary>
    /// Removes a library section and all associated metadata items.
    /// </summary>
    /// <param name="input">The library section to remove.</param>
    /// <param name="librarySectionService">The library section service.</param>
    /// <returns>Payload indicating success or error.</returns>
    /// <exception cref="GraphQLException">Thrown when validation fails.</exception>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<RemoveLibrarySectionPayload> RemoveLibrarySectionAsync(
        RemoveLibrarySectionInput input,
        [Service] ILibrarySectionService librarySectionService
    )
    {
        if (input is null)
        {
            throw CreateGraphQLInputError("Library section input is required.");
        }

        if (input.LibrarySectionId == Guid.Empty)
        {
            throw CreateGraphQLInputError("Library section id is required.");
        }

        bool removed = await librarySectionService.RemoveLibrarySectionAsync(
            input.LibrarySectionId
        );

        return removed
            ? new RemoveLibrarySectionPayload(true)
            : new RemoveLibrarySectionPayload(false, "Library section not found.");
    }

    /// <summary>
    /// Promotes a metadata item to the hero carousel.
    /// </summary>
    /// <param name="input">The promote item input containing the item ID and optional expiration.</param>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success or error.</returns>
    /// <exception cref="GraphQLException">Thrown when validation fails.</exception>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<PromoteItemPayload> PromoteItemAsync(
        PromoteItemInput input,
        [Service] IMetadataItemRepository metadataItemRepository,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePromotionGraphQLInputError("Promote item input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            throw CreatePromotionGraphQLInputError("Metadata item id is required.");
        }

        var item = await metadataItemRepository.GetByUuidAsync(input.ItemId);

        if (item is null)
        {
            return new PromoteItemPayload(false, "Metadata item not found.");
        }

        // Only root items can be promoted
        if (item.ParentId is not null)
        {
            return new PromoteItemPayload(
                false,
                "Only root items (movies, shows, albums) can be promoted."
            );
        }

        // Check if already promoted
        if (item.IsPromoted)
        {
            return new PromoteItemPayload(false, "Item is already promoted.");
        }

        item.IsPromoted = true;
        item.PromotedAt = DateTime.UtcNow;
        item.PromotedUntil = input.PromotedUntil;

        await metadataItemRepository.UpdateAsync(item);

        return new PromoteItemPayload(true);
    }

    /// <summary>
    /// Unpromotes a metadata item from the hero carousel.
    /// </summary>
    /// <param name="input">The unpromote item input containing the item ID.</param>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payload indicating success or error.</returns>
    /// <exception cref="GraphQLException">Thrown when validation fails.</exception>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<PromoteItemPayload> UnpromoteItemAsync(
        UnpromoteItemInput input,
        [Service] IMetadataItemRepository metadataItemRepository,
        CancellationToken cancellationToken
    )
    {
        if (input is null)
        {
            throw CreatePromotionGraphQLInputError("Unpromote item input is required.");
        }

        if (input.ItemId == Guid.Empty)
        {
            throw CreatePromotionGraphQLInputError("Metadata item id is required.");
        }

        var item = await metadataItemRepository.GetByUuidAsync(input.ItemId);

        if (item is null)
        {
            return new PromoteItemPayload(false, "Metadata item not found.");
        }

        // Check if not promoted
        if (!item.IsPromoted)
        {
            return new PromoteItemPayload(false, "Item is not currently promoted.");
        }

        item.IsPromoted = false;
        item.PromotedAt = null;
        item.PromotedUntil = null;

        await metadataItemRepository.UpdateAsync(item);

        return new PromoteItemPayload(true);
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

    private static GraphQLException CreateMetadataGraphQLInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage(message)
                .SetCode("METADATA_REFRESH_VALIDATION_ERROR")
                .Build()
        );
    }

    private static GraphQLException CreatePromotionGraphQLInputError(string message)
    {
        return new GraphQLException(
            ErrorBuilder.New().SetMessage(message).SetCode("PROMOTION_VALIDATION_ERROR").Build()
        );
    }
}
