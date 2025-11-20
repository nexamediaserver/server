// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Images;

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
    private readonly IFileAnalyzer[] fileAnalyzers;
    private readonly IImageProvider[] imageProviders;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<MetadataService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataService"/> class.
    /// </summary>
    /// <param name="itemService">The low-level item service used for database access.</param>
    /// <param name="agents">The discovered metadata agents.</param>
    /// <param name="libraryRepository">The library repository.</param>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="imageService">The image service for image handling.</param>
    /// <param name="fileAnalyzers">Discovered file analyzers.</param>
    /// <param name="imageProviders">Discovered image providers.</param>
    /// <param name="jobClient">Hangfire job client for enqueueing follow-up jobs.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    public MetadataService(
        IMetadataItemService itemService,
        IEnumerable<IMetadataAgent> agents,
        ILibrarySectionRepository libraryRepository,
        IMetadataItemRepository metadataItemRepository,
        IImageService imageService,
        IEnumerable<IFileAnalyzer> fileAnalyzers,
        IEnumerable<IImageProvider> imageProviders,
        IBackgroundJobClient jobClient,
        ILogger<MetadataService> logger
    )
    {
        this.itemService = itemService;
        this.agents = agents?.ToArray() ?? Array.Empty<IMetadataAgent>();
        this.libraryRepository = libraryRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.imageService = imageService;
        this.fileAnalyzers = fileAnalyzers?.ToArray() ?? Array.Empty<IFileAnalyzer>();
        this.imageProviders = imageProviders?.ToArray() ?? Array.Empty<IImageProvider>();
        this.jobClient = jobClient;
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

    /// <summary>
    /// Hangfire job: refresh metadata for a single metadata item using discovered agents.
    /// Executes on the "metadata_agents" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("metadata_agents")]
    [MaximumConcurrentExecutions(3, timeoutInSeconds: 300, pollingIntervalInSeconds: 5)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RefreshMetadataAsync(Guid metadataItemUuid)
    {
        this.LogRefreshMetadataStarted(metadataItemUuid);

        var item = await this.metadataItemRepository.GetByUuidAsync(metadataItemUuid);
        if (item == null)
        {
            this.LogMetadataItemNotFound(metadataItemUuid);
            return; // Could log missing item
        }

        var library = await this.libraryRepository.GetByIdAsync(item.LibrarySectionId);
        if (library == null)
        {
            this.LogLibraryNotFound(item.LibrarySectionId, metadataItemUuid);
            return; // Could log missing library
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

        // Run all agents in parallel (local + remote) collecting their results with timing & detailed logs.
        async Task<NexaMediaServer.Core.DTOs.AgentMetadataResult?> RunAgentAsync(
            IMetadataAgent agent
        )
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
                        CancellationToken.None
                    ),
                    IRemoteMetadataAgent remote => remote.GetMetadataAsync(
                        item,
                        library,
                        CancellationToken.None
                    ),
                    _ => Task.FromResult<NexaMediaServer.Core.DTOs.AgentMetadataResult?>(null),
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
        this.LogAgentsCompleted(metadataItemUuid, results.Length);

        // NOTE: Merge results, apply precedence, update item fields and persist changes.
        // Implementation of merging, conflict resolution, localization, external id mapping, etc. remains.

        // After metadata enrichment completes, enqueue file analysis job.
        this.jobClient.Enqueue<MetadataService>(svc => svc.AnalyzeFilesAsync(metadataItemUuid));
        this.LogEnqueueFileAnalysis(metadataItemUuid);
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
            var analyzerTasks = this
                .fileAnalyzers.Where(analyzer => analyzer.Supports(media))
                .Select(async analyzer =>
                {
                    try
                    {
                        this.LogRunningAnalyzer(analyzer.Name, media.Id);
                        return await analyzer
                            .AnalyzeAsync(media, meta, parts, CancellationToken.None)
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

        // Media items are already tracked and modified in memory.
        // Persist changes from file analysis.
        await this.metadataItemRepository.UpdateAsync(meta);
        this.LogMediaItemsAnalyzed(mediaItems.Count, metadataItemUuid);

        // After file analysis completes, enqueue image generation job.
        this.jobClient.Enqueue<MetadataService>(svc => svc.GenerateImagesAsync(metadataItemUuid));
        this.LogEnqueueImageGeneration(metadataItemUuid);

        this.LogAnalyzeFilesCompleted(metadataItemUuid);
    }

    /// <summary>
    /// Hangfire job: generate images (artwork and thumbnails) for a metadata item using image providers.
    /// Executes on the "image_generators" queue after file analysis.
    /// Excludes trickplay generation which runs in a separate job after artwork persistence.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("image_generators")]
    [AutomaticRetry(Attempts = 0)]
    [MaximumConcurrentExecutions(2, timeoutInSeconds: 1800, pollingIntervalInSeconds: 5)]
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

        var mediaItems = meta.MediaItems?.ToList() ?? new List<MediaItem>();
        if (mediaItems.Count == 0)
        {
            this.LogNoMediaItemsFound(metadataItemUuid);
            return;
        }

        this.LogMediaItemsFoundForImageGeneration(mediaItems.Count, metadataItemUuid);

        // Process all media items in parallel
        var processingTasks = mediaItems.Select(async media =>
        {
            // Parts were eager-loaded; use in-memory collection
            var parts = media.Parts?.ToList() ?? new List<MediaPart>();

            this.LogGeneratingImagesForMediaItem(media.Id, metadataItemUuid);

            // Log provider order once per media item.
            if (this.imageProviders.Length > 0)
            {
                var providerOrder = string.Join(", ", this.imageProviders.Select(p => p.Name));
                this.LogImageProvidersOrdered(this.imageProviders.Length, providerOrder, media.Id);
            }

            // Run image providers respecting order, but exclude trickplay providers
            // Trickplay generation will run in a separate job after artwork is persisted
            foreach (var provider in this.imageProviders)
            {
                // Skip trickplay provider - it will run in GenerateTrickplayAsync after artwork
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
                        .ProvideAsync(media, meta, parts, CancellationToken.None)
                        .ConfigureAwait(false);
                    this.LogImageProviderFinished(
                        provider.Name,
                        media.Id,
                        swProvider.ElapsedMilliseconds
                    );
                }
                catch (Exception ex)
                {
                    this.LogImageProviderError(provider.Name, media.Id, ex);
                }
            }
        });

        // Wait for all media items to be processed
        await Task.WhenAll(processingTasks).ConfigureAwait(false);

        this.LogImagesGenerated(mediaItems.Count, metadataItemUuid);

        // After all image providers have executed, choose and persist primary artwork.
        try
        {
            var library = await this.libraryRepository.GetByIdAsync(meta.LibrarySectionId);
            // Build precedence list for artwork selection:
            // 1. Library-defined metadata agent order (if any) or discovered metadata agents.
            // 2. Image provider names (so provider-generated posters/backdrops can participate).
            var orderedAgentIdentifiers = new List<string>();
            if (library?.Settings.MetadataAgentOrder.Count > 0)
            {
                orderedAgentIdentifiers.AddRange(library.Settings.MetadataAgentOrder);
            }
            else
            {
                orderedAgentIdentifiers.AddRange(this.agents.Select(a => a.Name));
            }

            orderedAgentIdentifiers.AddRange(this.imageProviders.Select(p => p.Name));
            // Distinct (case-insensitive) while preserving first occurrence ordering.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            orderedAgentIdentifiers = orderedAgentIdentifiers
                .Where(n => !string.IsNullOrWhiteSpace(n) && seen.Add(n))
                .ToList();

            // Attempt to set primary poster, backdrop, and logo using precedence rules.
            await this.imageService.SetPrimaryArtworkAsync(
                meta,
                ArtworkKind.Poster,
                orderedAgentIdentifiers,
                CancellationToken.None
            );
            await this.imageService.SetPrimaryArtworkAsync(
                meta,
                ArtworkKind.Backdrop,
                orderedAgentIdentifiers,
                CancellationToken.None
            );
            await this.imageService.SetPrimaryArtworkAsync(
                meta,
                ArtworkKind.Logo,
                orderedAgentIdentifiers,
                CancellationToken.None
            );

            // Persist metadata item changes (artwork URIs).
            await this.metadataItemRepository.UpdateAsync(meta);
            this.LogPersistedPrimaryArtwork(meta.Uuid);
        }
        catch (Exception ex)
        {
            this.LogPersistPrimaryArtworkFailed(meta.Uuid, ex);
        }

        this.LogGenerateImagesCompleted(metadataItemUuid);

        // After artwork is persisted, enqueue trickplay generation job
        this.jobClient.Enqueue<MetadataService>(svc =>
            svc.GenerateTrickplayAsync(metadataItemUuid)
        );
        this.LogEnqueueTrickplayGeneration(metadataItemUuid);
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

        // Process all media items in parallel
        var processingTasks = mediaItems.Select(async media =>
        {
            // Parts were eager-loaded; use in-memory collection
            var parts = media.Parts?.ToList() ?? new List<MediaPart>();

            this.LogGeneratingTrickplayForMediaItem(media.Id, metadataItemUuid);

            // Find the trickplay provider
            var trickplayProvider = this.imageProviders.FirstOrDefault(p =>
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
                await trickplayProvider
                    .ProvideAsync(media, meta, parts, CancellationToken.None)
                    .ConfigureAwait(false);
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

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting metadata refresh for {MetadataItemUuid}"
    )]
    private partial void LogRefreshMetadataStarted(Guid metadataItemUuid);

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
        Message = "Ordered {Count} metadata agents: {AgentNames}"
    )]
    private partial void LogAgentsOrdered(int count, string agentNames);

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
    #endregion
}
