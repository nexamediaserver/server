// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service responsible for orchestrating image provider execution, artwork selection,
/// and image generation for metadata items.
/// </summary>
public interface IImageOrchestrationService
{
    /// <summary>
    /// Runs all applicable image providers for a metadata item's media items,
    /// excluding trickplay generation which runs separately.
    /// </summary>
    /// <param name="item">The metadata item to generate images for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of provider names that contributed images.</returns>
    Task<List<string>> RunImageProvidersAsync(
        MetadataItem item,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Selects and persists primary artwork (poster, backdrop, logo) based on precedence rules.
    /// </summary>
    /// <param name="item">The tracked metadata item.</param>
    /// <param name="additionalProviderNames">Optional additional provider names to include in precedence list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SelectAndPersistArtworkAsync(
        MetadataItem item,
        IEnumerable<string>? additionalProviderNames,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Ingests artwork from agent results into the media directory for later selection.
    /// </summary>
    /// <param name="item">The metadata item.</param>
    /// <param name="agentNames">Ordered list of agent names.</param>
    /// <param name="agentResults">Results from metadata agents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestAgentArtworkAsync(
        MetadataItem item,
        IReadOnlyList<string> agentNames,
        AgentMetadataResult?[] agentResults,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Ingests artwork from local metadata (sidecar or embedded) into the media directory.
    /// </summary>
    /// <param name="item">The metadata item.</param>
    /// <param name="metadata">The local metadata containing artwork URIs.</param>
    /// <param name="sourceIdentifier">Source identifier for precedence (e.g., "sidecar", "embedded").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if any artwork was ingested.</returns>
    Task<bool> IngestArtworkAsync(
        MetadataItem item,
        MetadataBaseItem? metadata,
        string sourceIdentifier,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Ingests artwork for person and group credits.
    /// </summary>
    /// <param name="people">Person credits with potential artwork.</param>
    /// <param name="groups">Group credits with potential artwork.</param>
    /// <param name="sourceIdentifier">Source identifier for precedence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestCreditArtworkAsync(
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        string sourceIdentifier,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Hangfire job: generate images (artwork and thumbnails) for a metadata item.
    /// Executes on the "image_generators" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GenerateImagesAsync(Guid metadataItemUuid);

    /// <summary>
    /// Hangfire job: generate trickplay (BIF) files for a metadata item.
    /// Executes on the "trickplay" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GenerateTrickplayAsync(Guid metadataItemUuid);
}
