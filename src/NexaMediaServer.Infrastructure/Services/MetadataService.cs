// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;


using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Music;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// High-level metadata service responsible for mapping, enrichment and future metadata agents.
/// </summary>
public sealed partial class MetadataService : IMetadataService
{
    private readonly IMetadataItemService itemService;
    private readonly IMetadataAgent[] agents;
    private readonly ILibrarySectionRepository libraryRepository;
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly IImageService imageService;
    private readonly IPartsRegistry partsRegistry;
    private readonly ISidecarMetadataService sidecarMetadataService;
    private readonly IBackgroundJobClient jobClient;
    private readonly MediaServerContext dbContext;
    private readonly IGenreNormalizationService genreNormalizationService;
    private readonly ITagModerationService tagModerationService;

    [SuppressMessage(
        "Style",
        "IDE0052",
        Justification = "Used by LoggerMessage source generators."
    )]
    private readonly ILogger<MetadataService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataService"/> class.
    /// </summary>
    /// <param name="itemService">The low-level item service used for database access.</param>
    /// <param name="agents">The discovered metadata agents.</param>
    /// <param name="libraryRepository">The library repository.</param>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="imageService">The image service for image handling.</param>
    /// <param name="partsRegistry">Registry providing typed analyzers and image providers.</param>
    /// <param name="sidecarMetadataService">Service for parsing sidecar files and extracting embedded metadata.</param>
    /// <param name="jobClient">Hangfire job client for enqueueing follow-up jobs.</param>
    /// <param name="dbContext">The database context for direct EF Core access.</param>
    /// <param name="genreNormalizationService">Service for normalizing genre names.</param>
    /// <param name="tagModerationService">Service for filtering tags via allowlist/blocklist.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S107",
        Justification = "Constructor parameter count reflects required dependencies."
    )]
    public MetadataService(
        IMetadataItemService itemService,
        IEnumerable<IMetadataAgent> agents,
        ILibrarySectionRepository libraryRepository,
        IMetadataItemRepository metadataItemRepository,
        IImageService imageService,
        IPartsRegistry partsRegistry,
        ISidecarMetadataService sidecarMetadataService,
        IBackgroundJobClient jobClient,
        MediaServerContext dbContext,
        IGenreNormalizationService genreNormalizationService,
        ITagModerationService tagModerationService,
        ILogger<MetadataService> logger
    )
    {
        this.itemService = itemService;
        this.agents = agents?.ToArray() ?? Array.Empty<IMetadataAgent>();
        this.libraryRepository = libraryRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.imageService = imageService;
        this.partsRegistry = partsRegistry;
        this.sidecarMetadataService = sidecarMetadataService;
        this.jobClient = jobClient;
        this.dbContext = dbContext;
        this.genreNormalizationService = genreNormalizationService;
        this.tagModerationService = tagModerationService;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the number of registered metadata agents. Temporary surface to avoid unused warnings until enrichment is implemented.
    /// </summary>
    public int AgentsCount => this.agents.Length;

    /// <inheritdoc />
    public IQueryable<MetadataItem> GetQueryable() => this.itemService.GetQueryable();

    /// <inheritdoc />
    public IQueryable<MetadataItem> GetLibraryRootsQueryable(Guid librarySectionId) =>
        this.itemService.GetLibraryRootsQueryable(librarySectionId);

    /// <inheritdoc />
    public Task<MetadataItem?> GetByUuidAsync(Guid id) => this.itemService.GetByUuidAsync(id);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<int, IReadOnlyList<MetadataItem>>> GetExtrasByOwnersAsync(
        IReadOnlyCollection<int> ownerMetadataIds,
        CancellationToken cancellationToken = default
    ) => this.metadataItemRepository.GetExtrasByOwnersAsync(ownerMetadataIds, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<int, IReadOnlyList<CastMember>>> GetCastByMetadataIdsAsync(
        IReadOnlyCollection<int> metadataIds,
        CancellationToken cancellationToken = default
    ) => this.metadataItemRepository.GetCastByMetadataIdsAsync(metadataIds, cancellationToken);

    /// <summary>
    /// Hangfire job: enrich metadata for a single metadata item using all available sources concurrently.
    /// Runs ISidecarParser, IMetadataAgent, and IImageProvider stages in parallel, then merges
    /// results according to precedence and persists once.
    /// Executes on the "metadata_agents" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the metadata item.</param>
    /// <param name="skipAnalysis">When <see langword="true"/>, skips enqueueing file analysis and trickplay generation jobs.
    /// Use for metadata-only refresh without triggering full media processing.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("metadata_agents")]
    [MaximumConcurrentExecutions(3, timeoutInSeconds: 600, pollingIntervalInSeconds: 5)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RefreshMetadataAsync(Guid metadataItemUuid, bool skipAnalysis = false)
    {
        this.LogRefreshMetadataStarted(metadataItemUuid);

        var item = await this
            .metadataItemRepository.GetTrackedQueryable()
            .Where(m => m.Uuid == metadataItemUuid)
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
        // Filter out any disabled agents. Silently skip agent names that no longer exist.
        var disabledAgents = new HashSet<string>(
            library.Settings.DisabledMetadataAgents,
            StringComparer.OrdinalIgnoreCase
        );

        var orderedAgents =
            library.Settings.MetadataAgentOrder.Count > 0
                ? library
                    .Settings.MetadataAgentOrder.Where(name => !disabledAgents.Contains(name))
                    .Select(name =>
                        this.agents.FirstOrDefault(a =>
                            string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .Where(a => a != null)
                    .Cast<IMetadataAgent>()
                    .ToList()
                : this.agents.Where(a => !disabledAgents.Contains(a.Name)).ToList();

        var agentNames = string.Join(", ", orderedAgents.Select(a => a.Name));
        this.LogAgentsOrdered(orderedAgents.Count, agentNames);

        // Resolve image providers upfront for concurrent execution.
        var metadataDto = MetadataItemMapper.Map(item);
        var imageProviders = this.ResolveImageProviders(metadataDto);

        this.LogConcurrentEnrichmentStarted(
            metadataItemUuid,
            orderedAgents.Count,
            imageProviders.Length
        );

        // Run all three enrichment stages CONCURRENTLY.
        // Each stage collects its results without persisting; we merge and persist once at the end.
        var sidecarTask = this.RunSidecarsInternalAsync(item, library, CancellationToken.None);
        var agentsTask = this.RunMetadataAgentsInternalAsync(
            item,
            library,
            orderedAgents,
            CancellationToken.None
        );
        var imageProvidersTask = this.RunImageProvidersInternalAsync(
            item,
            imageProviders,
            CancellationToken.None
        );

        await Task.WhenAll(sidecarTask, agentsTask, imageProvidersTask).ConfigureAwait(false);

        var sidecarResult = await sidecarTask.ConfigureAwait(false);
        var agentResults = await agentsTask.ConfigureAwait(false);
        var imageProviderNames = await imageProvidersTask.ConfigureAwait(false);

        this.LogConcurrentEnrichmentCompleted(metadataItemUuid);

        // Ingest agent-provided artwork (absolute paths or URLs) into the media directory for later selection.
        await this.IngestAgentArtworkAsync(
                item,
                orderedAgents,
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
            await this.UpsertCreditsAsync(item, agentPeople, agentGroups, CancellationToken.None)
                .ConfigureAwait(false);
        }

        // Process genres and tags from agents and sidecars.
        await this.ProcessGenresAndTagsAsync(
                item,
                agentResults,
                sidecarResult,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Build unified precedence list for artwork selection:
        // sidecar → agents → image providers → embedded
        var allProviderNames = orderedAgents.Select(a => a.Name).Concat(imageProviderNames);

        // Select and persist artwork ONCE after all sources have contributed.
        await this.SelectAndPersistArtworkAsync(item, allProviderNames, CancellationToken.None)
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
        this.jobClient.Enqueue<MetadataService>(svc => svc.AnalyzeFilesAsync(metadataItemUuid));
        this.LogEnqueueFileAnalysis(metadataItemUuid);

        // Only enqueue trickplay generation for items with video media files.
        if (HasVideoMediaParts(item))
        {
            this.jobClient.Enqueue<MetadataService>(svc =>
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
    /// Hangfire job: analyze media files for a metadata item and merge technical characteristics.
    /// Executes on the "file_analyzers" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("file_analyzers")]
    [AutomaticRetry(Attempts = 0)]
    [MaximumConcurrentExecutions(3, timeoutInSeconds: 1800, pollingIntervalInSeconds: 5)]
    public async Task AnalyzeFilesAsync(Guid metadataItemUuid)
    {
        this.LogAnalyzeFilesStarted(metadataItemUuid);

        // Load tracked metadata item including media items and parts.
        var meta = await this
            .metadataItemRepository.GetTrackedQueryable()
            .Where(m => m.Uuid == metadataItemUuid)
            .Include(m => m.MediaItems)
                .ThenInclude(mi => mi.Parts)
            .FirstOrDefaultAsync();

        if (meta == null)
        {
            this.LogMetadataItemNotFound(metadataItemUuid);
            return;
        }

        var mediaItems = meta.MediaItems?.ToList() ?? new List<MediaItem>();
        if (mediaItems.Count == 0)
        {
            this.LogNoMediaItemsFound(metadataItemUuid);
            return;
        }

        this.LogMediaItemsFound(mediaItems.Count, metadataItemUuid);
        var metadataDto = MetadataItemMapper.Map(meta);
        var fileAnalyzers = FileAnalyzerResolver.Resolve(metadataDto, this.partsRegistry);

        // PERFORMANCE OPTIMIZATION:
        // Process all media items in parallel instead of sequentially.
        // Each media item runs its analyzers in parallel as well.
        // MediaItems are tracked entities that will be persisted when we update the parent MetadataItem.
        var processingTasks = mediaItems.Select(async media =>
        {
            // Parts were eager-loaded; use in-memory collection
            var parts = media.Parts?.ToList() ?? new List<MediaPart>();

            this.LogAnalyzingMediaItem(media.Id, parts.Count, metadataItemUuid);

            // Run supported analyzers in parallel instead of sequentially
            var analyzerTasks = fileAnalyzers
                .Where(analyzer => analyzer.Supports(media))
                .Select(async analyzer =>
                {
                    try
                    {
                        this.LogRunningAnalyzer(analyzer.Name, media.Id);
                        return await analyzer
                            .Analyze(media, parts, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.LogAnalyzerError(analyzer.Name, media.Id, ex);
                        return null;
                    }
                });

            var contributions = (await Task.WhenAll(analyzerTasks).ConfigureAwait(false))
                .Where(r => r != null)
                .Cast<FileAnalysisResult>()
                .ToList();

            // Merge contributions: last-writer-wins for scalar fields, union for collections.
            if (contributions.Count > 0)
            {
                this.LogMergeContributions(media.Id, contributions.Count);
                long? fileSize = contributions
                    .Select(c => c.FileSizeBytes)
                    .LastOrDefault(c => c.HasValue);
                string? fileFormat = contributions
                    .Select(c => c.FileFormat)
                    .LastOrDefault(f => f != null);
                TimeSpan? duration = contributions
                    .Select(c => c.Duration)
                    .LastOrDefault(d => d.HasValue);
                bool? hasChapters = contributions
                    .Select(c => c.HasChapters)
                    .LastOrDefault(h => h.HasValue);
                int? chapterCount = contributions
                    .Select(c => c.ChapterCount)
                    .LastOrDefault(h => h.HasValue);
                bool? isDisc = contributions.Select(c => c.IsDisc).LastOrDefault(h => h.HasValue);
                string? discTitle = contributions
                    .Select(c => c.DiscTitle)
                    .LastOrDefault(f => f != null);
                string? discId = contributions.Select(c => c.DiscId).LastOrDefault(f => f != null);
                string? videoCodec = contributions
                    .Select(c => c.VideoCodec)
                    .LastOrDefault(f => f != null);
                string? videoCodecProfile = contributions
                    .Select(c => c.VideoCodecProfile)
                    .LastOrDefault(f => f != null);
                string? videoCodecLevel = contributions
                    .Select(c => c.VideoCodecLevel)
                    .LastOrDefault(f => f != null);
                int? videoBitrate = contributions
                    .Select(c => c.VideoBitrate)
                    .LastOrDefault(v => v.HasValue);
                int? videoWidth = contributions
                    .Select(c => c.VideoWidth)
                    .LastOrDefault(v => v.HasValue);
                int? videoHeight = contributions
                    .Select(c => c.VideoHeight)
                    .LastOrDefault(v => v.HasValue);
                string? videoAspectRatio = contributions
                    .Select(c => c.VideoAspectRatio)
                    .LastOrDefault(f => f != null);
                double? videoFrameRate = contributions
                    .Select(c => c.VideoFrameRate)
                    .LastOrDefault(v => v.HasValue);
                bool? videoIsInterlaced = contributions
                    .Select(c => c.VideoIsInterlaced)
                    .LastOrDefault(v => v.HasValue);
                int? videoBitDepth = contributions
                    .Select(c => c.VideoBitDepth)
                    .LastOrDefault(v => v.HasValue);
                string? videoColorSpace = contributions
                    .Select(c => c.VideoColorSpace)
                    .LastOrDefault(f => f != null);
                string? videoColorPrimaries = contributions
                    .Select(c => c.VideoColorPrimaries)
                    .LastOrDefault(f => f != null);
                string? videoColorTransfer = contributions
                    .Select(c => c.VideoColorTransfer)
                    .LastOrDefault(f => f != null);
                string? videoColorRange = contributions
                    .Select(c => c.VideoColorRange)
                    .LastOrDefault(f => f != null);
                string? videoDynamicRange = contributions
                    .Select(c => c.VideoDynamicRange)
                    .LastOrDefault(f => f != null);

                media.FileSizeBytes = fileSize ?? media.FileSizeBytes;
                media.FileFormat = fileFormat ?? media.FileFormat;
                media.Duration = duration ?? media.Duration;
                media.HasChapters = hasChapters ?? media.HasChapters;
                media.ChapterCount = chapterCount ?? media.ChapterCount;
                media.IsDisc = isDisc ?? media.IsDisc;
                media.DiscTitle = discTitle ?? media.DiscTitle;
                media.DiscId = discId ?? media.DiscId;
                media.VideoCodec = videoCodec ?? media.VideoCodec;
                media.VideoCodecProfile = videoCodecProfile ?? media.VideoCodecProfile;
                media.VideoCodecLevel = videoCodecLevel ?? media.VideoCodecLevel;
                media.VideoBitrate = videoBitrate ?? media.VideoBitrate;
                media.VideoWidth = videoWidth ?? media.VideoWidth;
                media.VideoHeight = videoHeight ?? media.VideoHeight;
                media.VideoAspectRatio = videoAspectRatio ?? media.VideoAspectRatio;
                media.VideoFrameRate = videoFrameRate ?? media.VideoFrameRate;
                media.VideoIsInterlaced = videoIsInterlaced ?? media.VideoIsInterlaced;
                media.VideoBitDepth = videoBitDepth ?? media.VideoBitDepth;
                media.VideoColorSpace = videoColorSpace ?? media.VideoColorSpace;
                media.VideoColorPrimaries = videoColorPrimaries ?? media.VideoColorPrimaries;
                media.VideoColorTransfer = videoColorTransfer ?? media.VideoColorTransfer;
                media.VideoColorRange = videoColorRange ?? media.VideoColorRange;
                media.VideoDynamicRange = videoDynamicRange ?? media.VideoDynamicRange;

                // Merge collections (union preserving existing values)
                static void MergeCollection<T>(
                    ICollection<T> target,
                    IEnumerable<ICollection<T>?> source
                )
                {
                    var union = new HashSet<T>(target);
                    foreach (var col in source)
                    {
                        if (col == null)
                        {
                            continue;
                        }

                        foreach (var v in col)
                        {
                            union.Add(v);
                        }
                    }

                    target.Clear();
                    foreach (var v in union)
                    {
                        target.Add(v);
                    }
                }

                MergeCollection(media.AudioCodecs, contributions.Select(c => c.AudioCodecs));
                MergeCollection(media.AudioLanguages, contributions.Select(c => c.AudioLanguages));
                MergeCollection(
                    media.AudioChannelCounts,
                    contributions.Select(c => c.AudioChannelCounts)
                );
                MergeCollection(
                    media.AudioSampleRates,
                    contributions.Select(c => c.AudioSampleRates)
                );
                MergeCollection(media.AudioBitDepths, contributions.Select(c => c.AudioBitDepths));
                MergeCollection(media.AudioBitrates, contributions.Select(c => c.AudioBitrates));
                media.AudioTrackCount =
                    contributions.Select(c => c.AudioTrackCount).LastOrDefault(v => v.HasValue)
                    ?? media.AudioTrackCount;
                media.SubtitleTrackCount =
                    contributions.Select(c => c.SubtitleTrackCount).LastOrDefault(v => v.HasValue)
                    ?? media.SubtitleTrackCount;
                MergeCollection(
                    media.SubtitleLanguages,
                    contributions.Select(c => c.SubtitleLanguages)
                );
                MergeCollection(
                    media.SubtitleFormats,
                    contributions.Select(c => c.SubtitleFormats)
                );
                media.ImageWidth =
                    contributions.Select(c => c.ImageWidth).LastOrDefault(v => v.HasValue)
                    ?? media.ImageWidth;
                media.ImageHeight =
                    contributions.Select(c => c.ImageHeight).LastOrDefault(v => v.HasValue)
                    ?? media.ImageHeight;
                media.ImageBitDepth =
                    contributions.Select(c => c.ImageBitDepth).LastOrDefault(v => v.HasValue)
                    ?? media.ImageBitDepth;
                media.ImageColorSpace =
                    contributions.Select(c => c.ImageColorSpace).LastOrDefault(f => f != null)
                    ?? media.ImageColorSpace;
                media.ImageHasExif =
                    contributions.Select(c => c.ImageHasExif).LastOrDefault(v => v.HasValue)
                    ?? media.ImageHasExif;
                media.ImageDateTaken =
                    contributions.Select(c => c.ImageDateTaken).LastOrDefault(v => v.HasValue)
                    ?? media.ImageDateTaken;
                media.DocumentPageCount =
                    contributions.Select(c => c.DocumentPageCount).LastOrDefault(v => v.HasValue)
                    ?? media.DocumentPageCount;
                media.DocumentWordCount =
                    contributions.Select(c => c.DocumentWordCount).LastOrDefault(v => v.HasValue)
                    ?? media.DocumentWordCount;
                media.DocumentHasImages =
                    contributions.Select(c => c.DocumentHasImages).LastOrDefault(v => v.HasValue)
                    ?? media.DocumentHasImages;
                MergeCollection(media.GamePlatforms, contributions.Select(c => c.GamePlatforms));
                MergeCollection(media.GameRegions, contributions.Select(c => c.GameRegions));
                media.GameIsRom =
                    contributions.Select(c => c.GameIsRom).LastOrDefault(v => v.HasValue)
                    ?? media.GameIsRom;
            }

            // Note: Media item will be bulk updated at the end instead of individual updates
        });

        // Wait for all media items to be processed
        await Task.WhenAll(processingTasks).ConfigureAwait(false);

        // Propagate analyzed durations to the parent metadata item (stored in seconds)
        int? mediaDurationSeconds = mediaItems
            .Select(mi => mi.Duration)
            .Where(d => d.HasValue && d.Value > TimeSpan.Zero)
            .Select(d => d!.Value)
            .Max(d => (int?)Math.Max(1, (int)Math.Round(d.TotalSeconds)));

        if (mediaDurationSeconds.HasValue && meta.Duration != mediaDurationSeconds)
        {
            meta.Duration = mediaDurationSeconds.Value;
        }

        // Media items are already tracked and modified in memory.
        // Persist changes from file analysis.
        await this.metadataItemRepository.UpdateAsync(meta);
        this.LogMediaItemsAnalyzed(mediaItems.Count, metadataItemUuid);

        this.LogAnalyzeFilesCompleted(metadataItemUuid);
    }

    /// <summary>
    /// Hangfire job: generate images (artwork and thumbnails) for a metadata item using image providers.
    /// Executes on the "image_generators" queue.
    /// This job is primarily for standalone/manual image regeneration. During normal scanning,
    /// image generation runs concurrently within RefreshMetadataAsync.
    /// Excludes trickplay generation which runs in a separate job.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("image_generators")]
    [AutomaticRetry(Attempts = 0)]
    [MaximumConcurrentExecutions(3, timeoutInSeconds: 1800, pollingIntervalInSeconds: 5)]
    public async Task GenerateImagesAsync(Guid metadataItemUuid)
    {
        this.LogGenerateImagesStarted(metadataItemUuid);

        // Load tracked metadata item including media items and parts.
        var meta = await this
            .metadataItemRepository.GetTrackedQueryable()
            .Where(m => m.Uuid == metadataItemUuid)
            .Include(m => m.MediaItems)
                .ThenInclude(mi => mi.Parts)
            .FirstOrDefaultAsync();

        if (meta == null)
        {
            this.LogMetadataItemNotFound(metadataItemUuid);
            return;
        }

        var metadataDto = MetadataItemMapper.Map(meta);
        var imageProviders = this.ResolveImageProviders(metadataDto);

        // Reuse the internal helper for image generation.
        var providerNames = await this.RunImageProvidersInternalAsync(
                meta,
                imageProviders,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        // Select and persist artwork including image provider names in precedence list.
        await this.SelectAndPersistArtworkAsync(meta, providerNames, CancellationToken.None)
            .ConfigureAwait(false);

        this.LogGenerateImagesCompleted(metadataItemUuid);

        // Only enqueue trickplay generation for items with video media files.
        if (HasVideoMediaParts(meta))
        {
            this.jobClient.Enqueue<MetadataService>(svc =>
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
    /// Hangfire job: generate trickplay (BIF) files for a metadata item.
    /// Executes on the "trickplay" queue after artwork and thumbnails are persisted.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("trickplay")]
    [AutomaticRetry(Attempts = 0)]
    [MaximumConcurrentExecutions(1, timeoutInSeconds: 3600, pollingIntervalInSeconds: 10)]
    public async Task GenerateTrickplayAsync(Guid metadataItemUuid)
    {
        this.LogGenerateTrickplayStarted(metadataItemUuid);

        // Load tracked metadata item including media items and parts.
        var meta = await this
            .metadataItemRepository.GetTrackedQueryable()
            .Where(m => m.Uuid == metadataItemUuid)
            .Include(m => m.MediaItems)
                .ThenInclude(mi => mi.Parts)
            .FirstOrDefaultAsync();

        if (meta == null)
        {
            this.LogMetadataItemNotFound(metadataItemUuid);
            return;
        }

        var mediaItems = meta.MediaItems?.ToList() ?? new List<MediaItem>();
        if (mediaItems.Count == 0)
        {
            this.LogNoMediaItemsFound(metadataItemUuid);
            return;
        }

        this.LogMediaItemsFoundForTrickplay(mediaItems.Count, metadataItemUuid);
        var metadataDto = MetadataItemMapper.Map(meta);
        var imageProviders = this.ResolveImageProviders(metadataDto);

        // Process all media items in parallel
        var processingTasks = mediaItems.Select(async media =>
        {
            // Parts were eager-loaded; use in-memory collection
            var parts = media.Parts?.ToList() ?? new List<MediaPart>();

            this.LogGeneratingTrickplayForMediaItem(media.Id, metadataItemUuid);

            // Find the trickplay provider
            var trickplayProvider = imageProviders.FirstOrDefault(p =>
                p.Name == "Trickplay BIF Generator"
            );

            if (trickplayProvider == null)
            {
                this.LogTrickplayProviderNotFound(media.Id);
                return;
            }

            var supported = false;
            try
            {
                supported = trickplayProvider.Supports(media);
            }
            catch (Exception ex)
            {
                this.LogImageProviderSupportCheckFailed(trickplayProvider.Name, media.Id, ex);
                return;
            }

            if (!supported)
            {
                this.LogImageProviderSkipped(trickplayProvider.Name, media.Id, "NotSupported");
                return;
            }

            var swProvider = Stopwatch.StartNew();
            try
            {
                this.LogRunningImageProvider(trickplayProvider.Name, media.Id);
                var provideTask = trickplayProvider.ProvideAsync(
                    media,
                    parts,
                    CancellationToken.None
                );
                await provideTask.ConfigureAwait(false);
                this.LogImageProviderFinished(
                    trickplayProvider.Name,
                    media.Id,
                    swProvider.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                this.LogImageProviderError(trickplayProvider.Name, media.Id, ex);
            }
        });

        // Wait for all media items to be processed
        await Task.WhenAll(processingTasks).ConfigureAwait(false);

        this.LogGenerateTrickplayCompleted(metadataItemUuid);
    }

    private static (
        List<PersonCredit> PersonCredits,
        List<GroupCredit> GroupCredits,
        List<string> PersonNames,
        List<string> GroupNames
    ) NormalizeCredits(IEnumerable<PersonCredit>? people, IEnumerable<GroupCredit>? groups)
    {
        var personCredits = (people ?? Enumerable.Empty<PersonCredit>()).ToList();
        var groupCredits = (groups ?? Enumerable.Empty<GroupCredit>()).ToList();

        if (groupCredits.Count > 0)
        {
            var memberCredits = groupCredits
                .SelectMany(g => g.Members ?? Array.Empty<PersonCredit>())
                .ToList();

            if (memberCredits.Count > 0)
            {
                personCredits.AddRange(memberCredits);
            }
        }

        personCredits = personCredits
            .Where(c => c.Person is not null && !string.IsNullOrWhiteSpace(c.Person.Title))
            .ToList();

        groupCredits = groupCredits
            .Where(c => c.Group is not null && !string.IsNullOrWhiteSpace(c.Group.Title))
            .ToList();

        var personNames = personCredits
            .Select(c => c.Person.Title.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupNames = groupCredits
            .Select(c => c.Group.Title.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (personCredits, groupCredits, personNames, groupNames);
    }

    private static List<MetadataItem> CreateMissingMetadata(
        List<PersonCredit> personCredits,
        List<GroupCredit> groupCredits,
        MetadataItem owner,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap
    )
    {
        var newMetadata = new List<MetadataItem>();

        foreach (var person in personCredits.Select(credit => credit.Person!))
        {
            var name = person.Title.Trim();
            if (personMap.ContainsKey(name))
            {
                continue;
            }

            var dto = person with
            {
                Title = name,
                SortTitle = string.IsNullOrWhiteSpace(person.SortTitle) ? name : person.SortTitle,
                LibrarySectionId = owner.LibrarySectionId,
            };

            var entity = MetadataItemMapper.MapToEntity(dto);
            personMap[name] = entity;
            newMetadata.Add(entity);
        }

        foreach (var group in groupCredits.Select(credit => credit.Group!))
        {
            var name = group.Title.Trim();
            if (groupMap.ContainsKey(name))
            {
                continue;
            }

            var dto = group with
            {
                Title = name,
                SortTitle = string.IsNullOrWhiteSpace(group.SortTitle) ? name : group.SortTitle,
                LibrarySectionId = owner.LibrarySectionId,
            };

            var entity = MetadataItemMapper.MapToEntity(dto);
            groupMap[name] = entity;
            newMetadata.Add(entity);
        }

        return newMetadata;
    }

    private static List<MetadataRelation> BuildRelationCandidates(
        List<PersonCredit> personCredits,
        List<GroupCredit> groupCredits,
        MetadataItem owner,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap
    )
    {
        var relationCandidates = new List<MetadataRelation>();

        foreach (var credit in groupCredits)
        {
            var groupName = credit.Group.Title.Trim();
            if (!groupMap.TryGetValue(groupName, out var group))
            {
                continue;
            }

            relationCandidates.Add(
                new MetadataRelation
                {
                    MetadataItemId = group.Id,
                    RelatedMetadataItemId = owner.Id,
                    RelationType = credit.RelationType,
                    Text = credit.Text,
                }
            );

            if (credit.Members is null)
            {
                continue;
            }

            foreach (var member in credit.Members)
            {
                var memberName = member.Person.Title.Trim();
                if (!personMap.TryGetValue(memberName, out var memberEntity))
                {
                    continue;
                }

                relationCandidates.Add(
                    new MetadataRelation
                    {
                        MetadataItemId = memberEntity.Id,
                        RelatedMetadataItemId = group.Id,
                        RelationType = member.RelationType,
                        Text = member.Text,
                    }
                );
            }
        }

        foreach (var credit in personCredits)
        {
            var personName = credit.Person.Title.Trim();
            if (!personMap.TryGetValue(personName, out var person))
            {
                continue;
            }

            relationCandidates.Add(
                new MetadataRelation
                {
                    MetadataItemId = person.Id,
                    RelatedMetadataItemId = owner.Id,
                    RelationType = credit.RelationType,
                    Text = credit.Text,
                }
            );
        }

        return relationCandidates;
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

    private static bool ShouldIncludeAgentIdentifier(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier)
        && !string.Equals(identifier, "sidecar", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(identifier, "embedded", StringComparison.OrdinalIgnoreCase);

#pragma warning disable SA1204 // Static members should appear before non-static members
    private static bool IsExtraMetadata(MetadataType metadataType) =>
        metadataType
            is MetadataType.Trailer
                or MetadataType.Clip
                or MetadataType.BehindTheScenes
                or MetadataType.DeletedScene
                or MetadataType.Featurette
                or MetadataType.Interview
                or MetadataType.Scene
                or MetadataType.ShortForm
                or MetadataType.ExtraOther;

    /// <summary>
    /// Runs sidecar and embedded metadata parsing, returning results without persisting.
    /// </summary>
    private async Task<SidecarEnrichmentResult> RunSidecarsInternalAsync(
        MetadataItem item,
        LibrarySection library,
        CancellationToken cancellationToken
    )
    {
        var result = new SidecarEnrichmentResult();

        if (IsExtraMetadata(item.MetadataType))
        {
            this.LogLocalMetadataSkipped(item.Uuid, "Local metadata disabled for extras");
            return result;
        }

        var mediaItems = item.MediaItems ?? new List<MediaItem>();
        var partPaths = mediaItems
            .SelectMany(media => media.Parts ?? Enumerable.Empty<MediaPart>())
            .Select(part => part.File)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (partPaths.Count == 0)
        {
            this.LogLocalMetadataSkipped(item.Uuid, "No media parts with file paths");
            return result;
        }

        foreach (var partPath in partPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mediaFile = FileSystemMetadata.FromPath(partPath);
            if (!mediaFile.Exists || mediaFile.IsDirectory)
            {
                continue;
            }

            this.LogApplyingLocalMetadata(item.Uuid, mediaFile.Path);

            // Delegate to SidecarMetadataService to handle all sidecar parsing
            var sidecarResult = await this.sidecarMetadataService.ExtractLocalMetadataAsync(
                item,
                library,
                null, // overrideFields not used in this context
                cancellationToken
            );

            // Merge results from SidecarMetadataService
            if (sidecarResult.People is { Count: > 0 })
            {
                result.People ??= new List<PersonCredit>();
                result.People.AddRange(sidecarResult.People);
            }

            if (sidecarResult.Groups is { Count: > 0 })
            {
                result.Groups ??= new List<GroupCredit>();
                result.Groups.AddRange(sidecarResult.Groups);
            }

            if (sidecarResult.Genres is { Count: > 0 })
            {
                result.Genres ??= new List<string>();
                result.Genres.AddRange(sidecarResult.Genres);
            }

            if (sidecarResult.Tags is { Count: > 0 })
            {
                result.Tags ??= new List<string>();
                result.Tags.AddRange(sidecarResult.Tags);
            }

            if (sidecarResult.LocalMetadataApplied)
            {
                result.LocalMetadataApplied = true;
            }
        }

        return result;
    }

    /// <summary>
    /// Runs all metadata agents in parallel, returning their results without persisting.
    /// </summary>
    private async Task<Core.DTOs.AgentMetadataResult?[]> RunMetadataAgentsInternalAsync(
        MetadataItem item,
        LibrarySection library,
        List<IMetadataAgent> orderedAgents,
        CancellationToken cancellationToken
    )
    {
        async Task<Core.DTOs.AgentMetadataResult?> RunAgentAsync(IMetadataAgent agent)
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
                    _ => Task.FromResult<Core.DTOs.AgentMetadataResult?>(null),
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

    /// <summary>
    /// Runs all image providers in parallel, returning provider names that contributed.
    /// Does not persist artwork selection; caller handles final merge.
    /// </summary>
    private async Task<List<string>> RunImageProvidersInternalAsync(
        MetadataItem item,
        ImageProviderAdapter[] imageProviders,
        CancellationToken cancellationToken
    )
    {
        var mediaItems = item.MediaItems?.ToList() ?? new List<MediaItem>();
        if (mediaItems.Count == 0)
        {
            this.LogNoMediaItemsFound(item.Uuid);
            return new List<string>();
        }

        this.LogMediaItemsFoundForImageGeneration(mediaItems.Count, item.Uuid);

        // Track which providers contributed.
        var contributedProviders = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Process all media items in parallel.
        var processingTasks = mediaItems.Select(async media =>
        {
            var parts = media.Parts?.ToList() ?? new List<MediaPart>();
            this.LogGeneratingImagesForMediaItem(media.Id, item.Uuid);

            if (imageProviders.Length > 0)
            {
                var providerOrder = string.Join(", ", imageProviders.Select(p => p.Name));
                this.LogImageProvidersOrdered(imageProviders.Length, providerOrder, media.Id);
            }

            foreach (var provider in imageProviders)
            {
                // Skip trickplay provider - it runs in a separate job after enrichment.
                if (provider.Name == "Trickplay BIF Generator")
                {
                    this.LogImageProviderSkipped(provider.Name, media.Id, "DeferredToTrickplayJob");
                    continue;
                }

                var supported = false;
                try
                {
                    supported = provider.Supports(media);
                }
                catch (Exception ex)
                {
                    this.LogImageProviderSupportCheckFailed(provider.Name, media.Id, ex);
                    continue;
                }

                if (!supported)
                {
                    this.LogImageProviderSkipped(provider.Name, media.Id, "NotSupported");
                    continue;
                }

                var swProvider = Stopwatch.StartNew();
                try
                {
                    this.LogRunningImageProvider(provider.Name, media.Id);
                    await provider
                        .ProvideAsync(media, parts, cancellationToken)
                        .ConfigureAwait(false);
                    this.LogImageProviderFinished(
                        provider.Name,
                        media.Id,
                        swProvider.ElapsedMilliseconds
                    );
                    contributedProviders.Add(provider.Name);
                }
                catch (Exception ex)
                {
                    this.LogImageProviderError(provider.Name, media.Id, ex);
                }
            }
        });

        await Task.WhenAll(processingTasks).ConfigureAwait(false);
        this.LogImagesGenerated(mediaItems.Count, item.Uuid);

        return contributedProviders.Distinct().ToList();
    }

    /// <summary>
    /// Selects and persists primary artwork for all artwork kinds.
    /// This method is called after each stage that may produce artwork to ensure
    /// images are displayed as early as possible.
    /// </summary>
    /// <param name="item">The tracked metadata item.</param>
    /// <param name="additionalProviderNames">Optional additional provider names to include in precedence list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    private async Task SelectAndPersistArtworkAsync(
        MetadataItem item,
        IEnumerable<string>? additionalProviderNames,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var library = await this.libraryRepository.GetByIdAsync(item.LibrarySectionId);

            // Build precedence list for artwork selection:
            // 1. Sidecar images (highest priority).
            // 2. Library-defined metadata agent order (if any) or discovered metadata agents.
            // 3. Image provider names (so provider-generated posters/backdrops can participate).
            // 4. Embedded images (lowest priority).
            var orderedAgentIdentifiers = new List<string> { "sidecar" };

            var agentIdentifiers =
                library?.Settings.MetadataAgentOrder.Count > 0
                    ? library.Settings.MetadataAgentOrder.ToList()
                    : this.agents.Select(a => a.Name).ToList();

            if (additionalProviderNames != null)
            {
                agentIdentifiers.AddRange(additionalProviderNames);
            }

            // Drop duplicate sidecar/embedded occurrences from the middle before de-duplication pass.
            var innerIdentifiers = agentIdentifiers.Where(ShouldIncludeAgentIdentifier);

            orderedAgentIdentifiers.AddRange(innerIdentifiers);
            orderedAgentIdentifiers.Add("embedded");

            // Distinct (case-insensitive) while preserving first occurrence ordering.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            orderedAgentIdentifiers = orderedAgentIdentifiers.Where(n => seen.Add(n)).ToList();

            // Attempt to set primary poster, backdrop, and logo using precedence rules.
            await this.imageService.SetPrimaryArtworkAsync(
                item,
                ArtworkKind.Poster,
                orderedAgentIdentifiers,
                cancellationToken
            );
            await this.imageService.SetPrimaryArtworkAsync(
                item,
                ArtworkKind.Backdrop,
                orderedAgentIdentifiers,
                cancellationToken
            );
            await this.imageService.SetPrimaryArtworkAsync(
                item,
                ArtworkKind.Logo,
                orderedAgentIdentifiers,
                cancellationToken
            );

            item.ThumbHash = await this
                .imageService.ComputeThumbHashAsync(
                    item.ThumbUri ?? string.Empty,
                    cancellationToken
                )
                .ConfigureAwait(false);
            item.ArtHash = await this
                .imageService.ComputeThumbHashAsync(item.ArtUri ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
            item.LogoHash = await this
                .imageService.ComputeThumbHashAsync(item.LogoUri ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);

            // Persist metadata item changes (artwork URIs).
            await this.metadataItemRepository.UpdateAsync(item);
            this.LogPersistedPrimaryArtwork(item.Uuid);
        }
        catch (Exception ex)
        {
            this.LogPersistPrimaryArtworkFailed(item.Uuid, ex);
        }
    }

    /// <summary>
    /// Processes genres and tags from agent metadata results and sidecars, applying normalization and moderation.
    /// </summary>
    private async Task ProcessGenresAndTagsAsync(
        MetadataItem item,
        Core.DTOs.AgentMetadataResult?[] agentResults,
        SidecarEnrichmentResult sidecarResult,
        CancellationToken cancellationToken
    )
    {
        // Collect all genres from agents.
        var allGenres = agentResults
            .Where(result => result?.Genres is { Count: > 0 })
            .SelectMany(result => result!.Genres!)
            .ToList();

        // Merge sidecar genres if present.
        if (sidecarResult.Genres is { Count: > 0 })
        {
            allGenres.AddRange(sidecarResult.Genres);
        }

        // Collect all tags from agents.
        var allTags = agentResults
            .Where(result => result?.Tags is { Count: > 0 })
            .SelectMany(result => result!.Tags!)
            .ToList();

        // Merge sidecar tags if present.
        if (sidecarResult.Tags is { Count: > 0 })
        {
            allTags.AddRange(sidecarResult.Tags);
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
        var genreEntities = new List<Genre>();
        foreach (var genreName in normalizedGenres)
        {
            var genre =
                await this.dbContext.Genres.FirstOrDefaultAsync(
                    g => g.Name == genreName,
                    cancellationToken
                ) ?? new Genre { Name = genreName };

            if (genre.Id == 0)
            {
                this.dbContext.Genres.Add(genre);
            }

            genreEntities.Add(genre);
        }

        // Query/create Tag entities.
        var tagEntities = new List<Tag>();
        foreach (var tagName in allowedTags)
        {
            var tag =
                await this.dbContext.Tags.FirstOrDefaultAsync(
                    t => t.Name == tagName,
                    cancellationToken
                ) ?? new Tag { Name = tagName };

            if (tag.Id == 0)
            {
                this.dbContext.Tags.Add(tag);
            }

            tagEntities.Add(tag);
        }

        // Save new genres/tags to get IDs assigned.
        await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Clear existing associations and assign new ones (leaf genres only).
        item.Genres.Clear();
        item.Tags.Clear();

        foreach (var genre in genreEntities)
        {
            item.Genres.Add(genre);
        }

        foreach (var tag in tagEntities)
        {
            item.Tags.Add(tag);
        }

        // Save item-to-genre and item-to-tag relationships.
        await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task IngestAgentArtworkAsync(
        MetadataItem item,
        List<IMetadataAgent> agents,
        Core.DTOs.AgentMetadataResult?[] results,
        CancellationToken cancellationToken
    )
    {
        var count = Math.Min(agents.Count, results.Length);
        for (var i = 0; i < count; i++)
        {
            var result = results[i];
            if (result?.Artwork is not { Count: > 0 })
            {
                continue;
            }

            foreach (var kvp in result.Artwork)
            {
                var ingested = await this
                    .imageService.IngestExternalArtworkAsync(
                        item,
                        agents[i].Name,
                        kvp.Key,
                        kvp.Value,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(ingested))
                {
                    result.Artwork[kvp.Key] = ingested;
                }
            }
        }
    }

    private ImageProviderAdapter[] ResolveImageProviders(MetadataBaseItem metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return metadata switch
        {
            Movie movie => this.CreateImageProviderAdapters(movie),
            Show show => this.CreateImageProviderAdapters(show),
            Season season => this.CreateImageProviderAdapters(season),
            Episode episode => this.CreateImageProviderAdapters(episode),
            Trailer trailer => this.CreateImageProviderAdapters(trailer),
            Clip clip => this.CreateImageProviderAdapters(clip),
            Video video => this.CreateImageProviderAdapters(video),
            AlbumReleaseGroup group => this.CreateImageProviderAdapters(group),
            AlbumRelease release => this.CreateImageProviderAdapters(release),
            Track track => this.CreateImageProviderAdapters(track),
            Recording recording => this.CreateImageProviderAdapters(recording),
            AudioWork work => this.CreateImageProviderAdapters(work),
            Photo photo => this.CreateImageProviderAdapters(photo),
            Picture picture => this.CreateImageProviderAdapters(picture),

            // Collection types use MetadataCollectionItem base for image providers
            MetadataCollectionItem collection => this.CreateImageProviderAdapters(collection),

            _ => Array.Empty<ImageProviderAdapter>(),
        };
    }

    private ImageProviderAdapter[] CreateImageProviderAdapters<TMetadata>(TMetadata metadata)
        where TMetadata : MetadataBaseItem
    {
        var providers = this.partsRegistry.GetImageProviders<TMetadata>();
        if (providers.Count == 0)
        {
            return Array.Empty<ImageProviderAdapter>();
        }

        return providers
            .Select(provider => ImageProviderAdapter.Create(metadata, provider))
            .ToArray();
    }

    private async Task<bool> UpsertCreditsAsync(
        MetadataItem owner,
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        CancellationToken cancellationToken
    )
    {
        var normalized = NormalizeCredits(people, groups);
        if (normalized.PersonCredits.Count == 0 && normalized.GroupCredits.Count == 0)
        {
            return false;
        }

        var personMap = await this.FetchExistingMetadataAsync(
                normalized.PersonNames,
                MetadataType.Person,
                cancellationToken
            )
            .ConfigureAwait(false);

        var groupMap = await this.FetchExistingMetadataAsync(
                normalized.GroupNames,
                MetadataType.Group,
                cancellationToken
            )
            .ConfigureAwait(false);

        var newMetadata = CreateMissingMetadata(
            normalized.PersonCredits,
            normalized.GroupCredits,
            owner,
            personMap,
            groupMap
        );

        var changes = await this.SaveNewMetadataAsync(
                newMetadata,
                personMap,
                groupMap,
                cancellationToken
            )
            .ConfigureAwait(false);

        var relationCandidates = BuildRelationCandidates(
            normalized.PersonCredits,
            normalized.GroupCredits,
            owner,
            personMap,
            groupMap
        );

        if (relationCandidates.Count == 0)
        {
            return changes;
        }

        var relationsInserted = await this.InsertMissingRelationsAsync(
                relationCandidates,
                cancellationToken
            )
            .ConfigureAwait(false);

        return changes || relationsInserted;
    }

    private async Task<Dictionary<string, MetadataItem>> FetchExistingMetadataAsync(
        List<string> names,
        MetadataType type,
        CancellationToken cancellationToken
    )
    {
        var map = new Dictionary<string, MetadataItem>(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0)
        {
            return map;
        }

        var query = this
            .dbContext.MetadataItems.AsNoTracking()
            .Where(m => m.MetadataType == type)
            .Where(m => names.Contains(EF.Functions.Collate(m.Title, "NOCASE")));

        var existing = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var meta in existing)
        {
            var name = (meta.Title ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                map[name] = meta;
            }
        }

        return map;
    }

    private async Task<bool> SaveNewMetadataAsync(
        List<MetadataItem> newMetadata,
        Dictionary<string, MetadataItem> personMap,
        Dictionary<string, MetadataItem> groupMap,
        CancellationToken cancellationToken
    )
    {
        if (newMetadata.Count == 0)
        {
            return false;
        }

        await this.dbContext.MetadataItems.AddRangeAsync(newMetadata, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        foreach (var meta in newMetadata)
        {
            var name = (meta.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (meta.MetadataType == MetadataType.Person)
            {
                personMap[name] = meta;
            }
            else if (meta.MetadataType == MetadataType.Group)
            {
                groupMap[name] = meta;
            }
        }

        return true;
    }

    private async Task<bool> InsertMissingRelationsAsync(
        IReadOnlyCollection<MetadataRelation> relationCandidates,
        CancellationToken cancellationToken
    )
    {
        var sourceIds = relationCandidates.Select(r => r.MetadataItemId).Distinct().ToList();
        var targetIds = relationCandidates.Select(r => r.RelatedMetadataItemId).Distinct().ToList();
        var relationTypes = relationCandidates.Select(r => r.RelationType).Distinct().ToList();

        var existingRelations = await this
            .dbContext.MetadataRelations.AsNoTracking()
            .Where(r => sourceIds.Contains(r.MetadataItemId))
            .Where(r => targetIds.Contains(r.RelatedMetadataItemId))
            .Where(r => relationTypes.Contains(r.RelationType))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingKeys = new HashSet<RelationKey>(RelationKeyComparer.Instance);
        foreach (var rel in existingRelations)
        {
            existingKeys.Add(
                new RelationKey(
                    rel.MetadataItemId,
                    rel.RelatedMetadataItemId,
                    rel.RelationType,
                    rel.Text
                )
            );
        }

        var toInsert = new List<MetadataRelation>();
        foreach (var candidate in relationCandidates)
        {
            var key = new RelationKey(
                candidate.MetadataItemId,
                candidate.RelatedMetadataItemId,
                candidate.RelationType,
                candidate.Text
            );

            if (existingKeys.Add(key))
            {
                toInsert.Add(candidate);
            }
        }

        if (toInsert.Count == 0)
        {
            return false;
        }

        await this.dbContext.MetadataRelations.AddRangeAsync(toInsert, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Sidecar parser {ParserName} finished for {SidecarPath} in {ElapsedMs}ms"
    )]
    private partial void LogSidecarParserFinished(
        string parserName,
        string sidecarPath,
        long elapsedMs
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sidecar parsing failed for {SidecarPath}")]
    private partial void LogSidecarParseFailed(string sidecarPath, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Embedded extractor {ExtractorName} finished for {MediaPath} in {ElapsedMs}ms"
    )]
    private partial void LogEmbeddedExtractorFinished(
        string extractorName,
        string mediaPath,
        long elapsedMs
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Embedded metadata extraction failed for {MediaPath}"
    )]
    private partial void LogEmbeddedExtractionFailed(string mediaPath, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting metadata refresh for {MetadataItemUuid}"
    )]
    private partial void LogRefreshMetadataStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting concurrent enrichment for {MetadataItemUuid}: {AgentCount} agents, {ProviderCount} image providers"
    )]
    private partial void LogConcurrentEnrichmentStarted(
        Guid metadataItemUuid,
        int agentCount,
        int providerCount
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Concurrent enrichment completed for {MetadataItemUuid}"
    )]
    private partial void LogConcurrentEnrichmentCompleted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Metadata item {MetadataItemUuid} not found for refresh/analyze"
    )]
    private partial void LogMetadataItemNotFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Library {LibrarySectionId} not found while refreshing metadata item {MetadataItemUuid}"
    )]
    private partial void LogLibraryNotFound(int librarySectionId, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applying local metadata for {MetadataItemUuid} from {MediaPath}"
    )]
    private partial void LogApplyingLocalMetadata(Guid metadataItemUuid, string mediaPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applied local metadata for {MetadataItemUuid}"
    )]
    private partial void LogLocalMetadataApplied(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipped local metadata for {MetadataItemUuid}: {Reason}"
    )]
    private partial void LogLocalMetadataSkipped(Guid metadataItemUuid, string reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Ordered {Count} metadata agents: {AgentNames}"
    )]
    private partial void LogAgentsOrdered(int count, string agentNames);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Year/ReleaseDate mismatch for '{Title}': Year={ProvidedYear}, ReleaseDate={ReleaseDate}. Using year derived from ReleaseDate: {DerivedYear}"
    )]
    private partial void LogYearReleaseDateMismatch(
        string title,
        int providedYear,
        DateOnly releaseDate,
        int derivedYear
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking metadata agent {AgentName}")]
    private partial void LogInvokingAgent(string agentName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Metadata agent {AgentName} finished in {ElapsedMs}ms result={HasResult}"
    )]
    private partial void LogAgentFinished(string agentName, long ElapsedMs, bool HasResult);

    [LoggerMessage(Level = LogLevel.Error, Message = "Metadata agent {AgentName} failed")]
    private partial void LogAgentFailed(string agentName, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed metadata agent execution for {MetadataItemUuid}. Results: {ResultCount}"
    )]
    private partial void LogAgentsCompleted(Guid metadataItemUuid, int resultCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Enqueued file analysis for {MetadataItemUuid}"
    )]
    private partial void LogEnqueueFileAnalysis(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping file analysis and trickplay generation for metadata-only refresh of {MetadataItemUuid}"
    )]
    private partial void LogSkippingAnalysis(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting file analysis for metadata item {MetadataItemUuid}"
    )]
    private partial void LogAnalyzeFilesStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {MediaItemCount} media items for metadata item {MetadataItemUuid}"
    )]
    private partial void LogMediaItemsFound(int mediaItemCount, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No media items found for metadata item {MetadataItemUuid}"
    )]
    private partial void LogNoMediaItemsFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analyzing media item {MediaItemId} with {PartCount} parts for metadata item {MetadataItemUuid}"
    )]
    private partial void LogAnalyzingMediaItem(
        int mediaItemId,
        int partCount,
        Guid metadataItemUuid
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Running analyzer {AnalyzerName} for media item {MediaItemId}"
    )]
    private partial void LogRunningAnalyzer(string analyzerName, int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Analyzer {AnalyzerName} failed for media item {MediaItemId}"
    )]
    private partial void LogAnalyzerError(string analyzerName, int mediaItemId, Exception? ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Merging {ContributionCount} analysis contributions for media item {MediaItemId}"
    )]
    private partial void LogMergeContributions(int mediaItemId, int contributionCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Persisted updated media item {MediaItemId}")]
    private partial void LogMediaItemUpdated(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analyzed {Count} media items for metadata item {MetadataItemUuid}, changes will be saved with MetadataItem update"
    )]
    private partial void LogMediaItemsAnalyzed(int count, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Running image provider {ProviderName} for media item {MediaItemId}"
    )]
    private partial void LogRunningImageProvider(string providerName, int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image providers ordered ({Count}): {ProviderNames} for media item {MediaItemId}"
    )]
    private partial void LogImageProvidersOrdered(int Count, string ProviderNames, int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image provider {ProviderName} skipped for media item {MediaItemId} reason={Reason}"
    )]
    private partial void LogImageProviderSkipped(
        string ProviderName,
        int MediaItemId,
        string Reason
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image provider {ProviderName} finished for media item {MediaItemId} in {ElapsedMs}ms"
    )]
    private partial void LogImageProviderFinished(
        string ProviderName,
        int MediaItemId,
        long ElapsedMs
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Image provider {ProviderName} support check failed for media item {MediaItemId}"
    )]
    private partial void LogImageProviderSupportCheckFailed(
        string ProviderName,
        int MediaItemId,
        Exception ex
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Image provider {ProviderName} failed for media item {MediaItemId}"
    )]
    private partial void LogImageProviderError(string providerName, int mediaItemId, Exception? ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed file analysis for metadata item {MetadataItemUuid}"
    )]
    private partial void LogAnalyzeFilesCompleted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Enqueued image generation for {MetadataItemUuid}"
    )]
    private partial void LogEnqueueImageGeneration(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting image generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateImagesStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {MediaItemCount} media items for image generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogMediaItemsFoundForImageGeneration(
        int mediaItemCount,
        Guid metadataItemUuid
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generating images for media item {MediaItemId} for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGeneratingImagesForMediaItem(int mediaItemId, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generated images for {Count} media items for metadata item {MetadataItemUuid}"
    )]
    private partial void LogImagesGenerated(int count, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed image generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateImagesCompleted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Persisted primary artwork for metadata item {MetadataItemUuid}"
    )]
    private partial void LogPersistedPrimaryArtwork(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to persist primary artwork for metadata item {MetadataItemUuid}"
    )]
    private partial void LogPersistPrimaryArtworkFailed(Guid metadataItemUuid, Exception? ex);

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

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting trickplay generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateTrickplayStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {MediaItemCount} media items for trickplay generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogMediaItemsFoundForTrickplay(int mediaItemCount, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generating trickplay for media item {MediaItemId} for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGeneratingTrickplayForMediaItem(int mediaItemId, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Trickplay BIF Generator provider not found for media item {MediaItemId}"
    )]
    private partial void LogTrickplayProviderNotFound(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed trickplay generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateTrickplayCompleted(Guid metadataItemUuid);

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
    #endregion

    /// <summary>
    /// Result container for sidecar/embedded enrichment stage.
    /// </summary>
    private sealed class SidecarEnrichmentResult
    {
        public bool LocalMetadataApplied { get; set; }
        public List<PersonCredit>? People { get; set; }
        public List<GroupCredit>? Groups { get; set; }
        public List<string>? Genres { get; set; }
        public List<string>? Tags { get; set; }
    }

    private sealed record RelationKey(
        int SourceId,
        int TargetId,
        RelationType RelationType,
        string? Text
    );

    private sealed class RelationKeyComparer : IEqualityComparer<RelationKey>
    {
        public static readonly RelationKeyComparer Instance = new();

        public bool Equals(RelationKey? x, RelationKey? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.SourceId == y.SourceId
                && x.TargetId == y.TargetId
                && x.RelationType == y.RelationType
                && string.Equals(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(RelationKey obj)
        {
            var hash = default(HashCode);
            hash.Add(obj.SourceId);
            hash.Add(obj.TargetId);
            hash.Add(obj.RelationType);
            hash.Add(obj.Text, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }


    private sealed class ImageProviderAdapter
    {
        private readonly Func<MediaItem, bool> supports;
        private readonly Func<MediaItem, IReadOnlyList<MediaPart>, CancellationToken, Task> provide;

        private ImageProviderAdapter(
            string name,
            int order,
            Func<MediaItem, bool> supports,
            Func<MediaItem, IReadOnlyList<MediaPart>, CancellationToken, Task> provide
        )
        {
            this.Name = name;
            this.Order = order;
            this.supports = supports;
            this.provide = provide;
        }

        public string Name { get; }

        public int Order { get; }

        public static ImageProviderAdapter Create<TMetadata>(
            TMetadata metadata,
            IImageProvider<TMetadata> provider
        )
            where TMetadata : MetadataBaseItem
        {
            return new ImageProviderAdapter(
                provider.Name,
                provider.Order,
                item => provider.Supports(item, metadata),
                (item, parts, token) => provider.ProvideAsync(item, metadata, parts, token)
            );
        }

        public bool Supports(MediaItem item) => this.supports(item);

        public Task ProvideAsync(
            MediaItem item,
            IReadOnlyList<MediaPart> parts,
            CancellationToken cancellationToken
        ) => this.provide(item, parts, cancellationToken);
    }
}
