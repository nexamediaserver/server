// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services.Agents;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Orchestrates the complete metadata refresh pipeline including local metadata extraction,
/// agent queries, image provider execution, credit upsert, and follow-up job scheduling.
/// </summary>
public sealed partial class MetadataRefreshOrchestrator : IMetadataRefreshOrchestrator
{
    private readonly ILibrarySectionRepository libraryRepository;
    private readonly ISidecarMetadataService sidecarMetadataService;
    private readonly IImageOrchestrationService imageOrchestrationService;
    private readonly ICreditService creditService;
    private readonly IGenreNormalizationService genreNormalizationService;
    private readonly ITagModerationService tagModerationService;
    private readonly MediaServerContext dbContext;
    private readonly IMetadataAgent[] agents;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<MetadataRefreshOrchestrator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataRefreshOrchestrator"/> class.
    /// </summary>
    /// <param name="libraryRepository">The library repository.</param>
    /// <param name="sidecarMetadataService">Service for sidecar and embedded metadata extraction.</param>
    /// <param name="imageOrchestrationService">Service for image provider orchestration.</param>
    /// <param name="creditService">Service for credit upsert.</param>
    /// <param name="genreNormalizationService">Service for normalizing genre names.</param>
    /// <param name="tagModerationService">Service for filtering tags via allowlist/blocklist.</param>
    /// <param name="dbContext">The database context for direct EF Core access.</param>
    /// <param name="agents">The discovered metadata agents.</param>
    /// <param name="jobClient">Hangfire job client for enqueueing follow-up jobs.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S107",
        Justification = "Constructor parameter count reflects required dependencies."
    )]
    public MetadataRefreshOrchestrator(
        ILibrarySectionRepository libraryRepository,
        ISidecarMetadataService sidecarMetadataService,
        IImageOrchestrationService imageOrchestrationService,
        ICreditService creditService,
        IGenreNormalizationService genreNormalizationService,
        ITagModerationService tagModerationService,
        MediaServerContext dbContext,
        IEnumerable<IMetadataAgent> agents,
        IBackgroundJobClient jobClient,
        ILogger<MetadataRefreshOrchestrator> logger
    )
    {
        this.libraryRepository = libraryRepository;
        this.sidecarMetadataService = sidecarMetadataService;
        this.imageOrchestrationService = imageOrchestrationService;
        this.creditService = creditService;
        this.genreNormalizationService = genreNormalizationService;
        this.tagModerationService = tagModerationService;
        this.dbContext = dbContext;
        this.agents = agents?.ToArray() ?? Array.Empty<IMetadataAgent>();
        this.jobClient = jobClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    [Queue("metadata_agents")]
    [MaximumConcurrentExecutions(3, timeoutInSeconds: 600, pollingIntervalInSeconds: 5)]
    [AutomaticRetry(Attempts = 0)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S3776",
        Justification = "Orchestrates concurrent enrichment stages; complexity inherent to coordination."
    )]
    public async Task RefreshMetadataAsync(
        Guid metadataItemUuid,
        bool skipAnalysis = false,
        IEnumerable<string>? overrideFields = null)
    {
        this.LogRefreshMetadataStarted(metadataItemUuid);

        // Materialize overrideFields to avoid multiple enumeration
        var overrideFieldsList = overrideFields?.ToList();

        var item = await this
            .dbContext.MetadataItems.Where(m => m.Uuid == metadataItemUuid)
            .Include(m => m.MediaItems)
                .ThenInclude(mi => mi.Parts)
            .Include(m => m.Genres)
            .Include(m => m.Tags)
            .FirstOrDefaultAsync();
        if (item == null)
        {
            this.LogMetadataItemNotFound(metadataItemUuid);
            return;
        }

        var library = await this.libraryRepository.GetByIdAsync(item.LibrarySectionId);
        if (library == null)
        {
            this.LogLibraryNotFound(item.LibrarySectionId, metadataItemUuid);
            return;
        }

        // Determine ordered list of agent names from library settings; fall back to all agents.
        var orderedAgents =
            library.Settings.MetadataAgentOrder.Count > 0
                ? library
                    .Settings.MetadataAgentOrder.Select(name =>
                        this.agents.FirstOrDefault(a =>
                            string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .Where(a => a != null)
                    .Cast<IMetadataAgent>()
                    .ToList()
                : this.agents.ToList();

        var agentNames = string.Join(", ", orderedAgents.Select(a => a.Name));
        this.LogAgentsOrdered(orderedAgents.Count, agentNames);

        this.LogConcurrentEnrichmentStarted(metadataItemUuid, orderedAgents.Count);

        // Run all three enrichment stages CONCURRENTLY.
        // Each stage collects its results without persisting; we merge and persist once at the end.
        var sidecarTask = this.sidecarMetadataService.ExtractLocalMetadataAsync(
            item,
            library,
            overrideFieldsList,
            CancellationToken.None
        );
        var agentsTask = this.RunMetadataAgentsInternalAsync(
            item,
            library,
            orderedAgents,
            CancellationToken.None
        );
        var imageProvidersTask = this.imageOrchestrationService.RunImageProvidersAsync(
            item,
            CancellationToken.None
        );

        await Task.WhenAll(sidecarTask, agentsTask, imageProvidersTask).ConfigureAwait(false);

        var sidecarResult = await sidecarTask.ConfigureAwait(false);
        var agentResults = await agentsTask.ConfigureAwait(false);
        var imageProviderNames = await imageProvidersTask.ConfigureAwait(false);

        this.LogConcurrentEnrichmentCompleted(metadataItemUuid);

        // Ingest agent-provided artwork (absolute paths or URLs) into the media directory for later selection.
        var orderedAgentNames = orderedAgents.Select(a => a.Name).ToList();
        await this
            .imageOrchestrationService.IngestAgentArtworkAsync(
                item,
                orderedAgentNames,
                agentResults,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Apply local metadata (sidecar > embedded) if available.
        if (sidecarResult.LocalMetadataApplied)
        {
            this.LogLocalMetadataApplied(item.Uuid);
        }

        // Collect people/groups from agents and sidecars for credit upsert.
        var agentPeople = agentResults
            .Where(result => result?.People is { Count: > 0 })
            .SelectMany(result => result!.People!)
            .ToList();

        var agentGroups = agentResults
            .Where(result => result?.Groups is { Count: > 0 })
            .SelectMany(result => result!.Groups!)
            .ToList();

        // Merge sidecar credits if present.
        if (sidecarResult.People is { Count: > 0 })
        {
            agentPeople.AddRange(sidecarResult.People);
        }

        if (sidecarResult.Groups is { Count: > 0 })
        {
            agentGroups.AddRange(sidecarResult.Groups);
        }

        if (agentPeople.Count > 0 || agentGroups.Count > 0)
        {
            await this
                .creditService.UpsertCreditsAsync(
                    item,
                    agentPeople,
                    agentGroups,
                    overrideFieldsList,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }

        // Process genres and tags from agents and sidecars.
        await this.ProcessGenresAndTagsAsync(
                item,
                agentResults,
                sidecarResult,
                overrideFieldsList,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Build unified precedence list for artwork selection:
        // sidecar → agents → image providers → embedded
        var allProviderNames = orderedAgentNames.Concat(imageProviderNames);

        // Select and persist artwork ONCE after all sources have contributed.
        await this
            .imageOrchestrationService.SelectAndPersistArtworkAsync(
                item,
                allProviderNames,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // NOTE: Merge results, apply precedence, update item fields and persist changes.
        // Implementation of merging, conflict resolution, localization, external id mapping, etc. remains.

        // Skip file analysis and trickplay generation for metadata-only refresh.
        if (skipAnalysis)
        {
            this.LogSkippingAnalysis(metadataItemUuid);
            return;
        }

        // After metadata enrichment completes, enqueue file analysis job.
        // Image generation already ran concurrently above.
        this.jobClient.Enqueue<IFileAnalysisOrchestrator>(svc =>
            svc.AnalyzeFilesAsync(metadataItemUuid)
        );
        this.LogEnqueueFileAnalysis(metadataItemUuid);

        // Only enqueue trickplay generation for items with video media files.
        if (HasVideoMediaParts(item))
        {
            this.jobClient.Enqueue<IImageOrchestrationService>(svc =>
                svc.GenerateTrickplayAsync(metadataItemUuid)
            );
            this.LogEnqueueTrickplayGeneration(metadataItemUuid);
        }
        else
        {
            this.LogSkipTrickplayNoVideoFiles(metadataItemUuid);
        }
    }

    /// <summary>
    /// Determines whether the metadata item contains any media parts with video file extensions.
    /// </summary>
    /// <param name="item">The metadata item to check.</param>
    /// <returns><c>true</c> if any media part has a video file extension; otherwise, <c>false</c>.</returns>
    private static bool HasVideoMediaParts(MetadataItem item)
    {
        if (item.MediaItems == null || item.MediaItems.Count == 0)
        {
            return false;
        }

        return item.MediaItems.Any(mi =>
            mi.Parts != null
            && mi.Parts.Any(p =>
                !string.IsNullOrWhiteSpace(p.File)
                && MediaFileExtensions.IsVideo(Path.GetExtension(p.File))
            )
        );
    }

    /// <summary>
    /// Runs all metadata agents in parallel, returning their results without persisting.
    /// </summary>
    private async Task<AgentMetadataResult?[]> RunMetadataAgentsInternalAsync(
        MetadataItem item,
        LibrarySection library,
        List<IMetadataAgent> orderedAgents,
        CancellationToken cancellationToken
    )
    {
        async Task<AgentMetadataResult?> RunAgentAsync(IMetadataAgent agent)
        {
            this.LogInvokingAgent(agent.Name);
            var sw = Stopwatch.StartNew();
            try
            {
                var task = agent switch
                {
                    ILocalMetadataAgent local => local.GetMetadataAsync(
                        item,
                        library,
                        cancellationToken
                    ),
                    IRemoteMetadataAgent remote => remote.GetMetadataAsync(
                        item,
                        library,
                        cancellationToken
                    ),
                    _ => Task.FromResult<AgentMetadataResult?>(null),
                };
                var result = await task.ConfigureAwait(false);
                this.LogAgentFinished(agent.Name, sw.ElapsedMilliseconds, result != null);
                return result;
            }
            catch (Exception ex)
            {
                this.LogAgentFailed(agent.Name, ex);
                return null;
            }
        }

        var tasks = orderedAgents.Select(RunAgentAsync);
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        this.LogAgentsCompleted(item.Uuid, results.Length);
        return results;
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting metadata refresh for {MetadataItemUuid}"
    )]
    private partial void LogRefreshMetadataStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Metadata item {MetadataItemUuid} not found for refresh"
    )]
    private partial void LogMetadataItemNotFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Library {LibrarySectionId} not found while refreshing metadata item {MetadataItemUuid}"
    )]
    private partial void LogLibraryNotFound(int librarySectionId, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Ordered {Count} metadata agents: {AgentNames}"
    )]
    private partial void LogAgentsOrdered(int count, string agentNames);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting concurrent enrichment for {MetadataItemUuid}: {AgentCount} agents"
    )]
    private partial void LogConcurrentEnrichmentStarted(Guid metadataItemUuid, int agentCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Concurrent enrichment completed for {MetadataItemUuid}"
    )]
    private partial void LogConcurrentEnrichmentCompleted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applied local metadata for {MetadataItemUuid}"
    )]
    private partial void LogLocalMetadataApplied(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping file analysis and trickplay generation for metadata-only refresh of {MetadataItemUuid}"
    )]
    private partial void LogSkippingAnalysis(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Enqueued file analysis for {MetadataItemUuid}"
    )]
    private partial void LogEnqueueFileAnalysis(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Enqueued trickplay generation for {MetadataItemUuid}"
    )]
    private partial void LogEnqueueTrickplayGeneration(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipped trickplay generation for {MetadataItemUuid} - no video files found"
    )]
    private partial void LogSkipTrickplayNoVideoFiles(Guid metadataItemUuid);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking metadata agent {AgentName}")]
    private partial void LogInvokingAgent(string agentName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Metadata agent {AgentName} finished in {ElapsedMs}ms result={HasResult}"
    )]
    private partial void LogAgentFinished(string agentName, long elapsedMs, bool hasResult);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Processing genres and tags for {MetadataItemUuid}: {GenreCount} genres, {TagCount} tags (before filtering)"
    )]
    private partial void LogGenresAndTagsProcessing(
        Guid metadataItemUuid,
        int genreCount,
        int tagCount
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "After normalization and filtering: {GenreCount} genres, {TagCount} tags for {MetadataItemUuid}"
    )]
    private partial void LogGenresAndTagsFiltered(
        Guid metadataItemUuid,
        int genreCount,
        int tagCount
    );

    /// <summary>
    /// Processes genres and tags from agent metadata results and sidecars, applying normalization and moderation.
    /// Respects field locks unless overridden.
    /// </summary>
    private async Task ProcessGenresAndTagsAsync(
        MetadataItem item,
        AgentMetadataResult?[] agentResults,
        SidecarEnrichmentResult sidecarResult,
        IEnumerable<string>? overrideFields,
        CancellationToken cancellationToken
    )
    {
        var overrideFieldsSet = overrideFields?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if genres are locked
        var genresLocked = item.LockedFields.Contains(
            Core.Constants.MetadataFieldNames.Genres,
            StringComparer.OrdinalIgnoreCase
        ) && overrideFieldsSet?.Contains(Core.Constants.MetadataFieldNames.Genres) != true;

        // Check if tags are locked
        var tagsLocked = item.LockedFields.Contains(
            Core.Constants.MetadataFieldNames.Tags,
            StringComparer.OrdinalIgnoreCase
        ) && overrideFieldsSet?.Contains(Core.Constants.MetadataFieldNames.Tags) != true;

        // If both are locked, skip processing entirely
        if (genresLocked && tagsLocked)
        {
            return;
        }

        // Collect all genres from agents (if not locked).
        var allGenres = new List<string>();
        if (!genresLocked)
        {
            allGenres = agentResults
                .Where(result => result?.Genres is { Count: > 0 })
                .SelectMany(result => result!.Genres!)
                .ToList();

            // Merge sidecar genres if present.
            if (sidecarResult.Genres is { Count: > 0 })
            {
                allGenres.AddRange(sidecarResult.Genres);
            }
        }

        // Collect all tags from agents (if not locked).
        var allTags = new List<string>();
        if (!tagsLocked)
        {
            allTags = agentResults
                .Where(result => result?.Tags is { Count: > 0 })
                .SelectMany(result => result!.Tags!)
                .ToList();

            // Merge sidecar tags if present.
            if (sidecarResult.Tags is { Count: > 0 })
            {
                allTags.AddRange(sidecarResult.Tags);
            }
        }

        this.LogGenresAndTagsProcessing(item.Uuid, allGenres.Count, allTags.Count);

        // Normalize genres and remove duplicates.
        var normalizedGenres = allGenres
            .Select(g => this.genreNormalizationService.NormalizeGenreName(g))
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Filter tags through moderation.
        var allowedTags = allTags
            .Where(t => !string.IsNullOrWhiteSpace(t) && this.tagModerationService.IsTagAllowed(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        this.LogGenresAndTagsFiltered(item.Uuid, normalizedGenres.Count, allowedTags.Count);

        // Query/create Genre entities using the shared database context.
        var genreEntities = new List<Core.Entities.Genre>();
        foreach (var genreName in normalizedGenres)
        {
            var genre =
                await this.dbContext.Genres.FirstOrDefaultAsync(
                    g => g.Name == genreName,
                    cancellationToken
                ) ?? new Core.Entities.Genre { Name = genreName };

            if (genre.Id == 0)
            {
                this.dbContext.Genres.Add(genre);
            }

            genreEntities.Add(genre);
        }

        // Query/create Tag entities.
        var tagEntities = new List<Core.Entities.Tag>();
        foreach (var tagName in allowedTags)
        {
            var tag =
                await this.dbContext.Tags.FirstOrDefaultAsync(
                    t => t.Name == tagName,
                    cancellationToken
                ) ?? new Core.Entities.Tag { Name = tagName };

            if (tag.Id == 0)
            {
                this.dbContext.Tags.Add(tag);
            }

            tagEntities.Add(tag);
        }

        // Save new genres/tags to get IDs assigned.
        await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Clear existing associations and assign new ones (only for unlocked collections).
        if (!genresLocked)
        {
            item.Genres.Clear();
            foreach (var genre in genreEntities)
            {
                item.Genres.Add(genre);
            }
        }

        if (!tagsLocked)
        {
            item.Tags.Clear();
            foreach (var tag in tagEntities)
            {
                item.Tags.Add(tag);
            }
        }

        // Save item-to-genre and item-to-tag relationships.
        await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Metadata agent {AgentName} failed")]
    private partial void LogAgentFailed(string agentName, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed metadata agent execution for {MetadataItemUuid}. Results: {ResultCount}"
    )]
    private partial void LogAgentsCompleted(Guid metadataItemUuid, int resultCount);
    #endregion
}
