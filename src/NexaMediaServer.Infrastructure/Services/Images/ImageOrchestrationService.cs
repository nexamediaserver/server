// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Parts;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Service responsible for orchestrating image provider execution, artwork selection,
/// and image generation for metadata items.
/// </summary>
public sealed partial class ImageOrchestrationService : IImageOrchestrationService
{
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly ILibrarySectionRepository libraryRepository;
    private readonly IImageService imageService;
    private readonly IPartsRegistry partsRegistry;
    private readonly IMetadataAgent[] agents;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<ImageOrchestrationService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageOrchestrationService"/> class.
    /// </summary>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="libraryRepository">The library repository.</param>
    /// <param name="imageService">The image service for image handling.</param>
    /// <param name="partsRegistry">Registry providing typed image providers.</param>
    /// <param name="agents">The discovered metadata agents.</param>
    /// <param name="jobClient">Hangfire job client for enqueueing follow-up jobs.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    public ImageOrchestrationService(
        IMetadataItemRepository metadataItemRepository,
        ILibrarySectionRepository libraryRepository,
        IImageService imageService,
        IPartsRegistry partsRegistry,
        IEnumerable<IMetadataAgent> agents,
        IBackgroundJobClient jobClient,
        ILogger<ImageOrchestrationService> logger
    )
    {
        this.metadataItemRepository = metadataItemRepository;
        this.libraryRepository = libraryRepository;
        this.imageService = imageService;
        this.partsRegistry = partsRegistry;
        this.agents = agents?.ToArray() ?? Array.Empty<IMetadataAgent>();
        this.jobClient = jobClient;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> RunImageProvidersAsync(
        MetadataItem item,
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

        var metadataDto = MetadataItemMapper.Map(item);
        var imageProviders = this.ResolveImageProviders(metadataDto);

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

    /// <inheritdoc />
    public async Task SelectAndPersistArtworkAsync(
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

    /// <inheritdoc />
    public async Task IngestAgentArtworkAsync(
        MetadataItem item,
        IReadOnlyList<string> agentNames,
        AgentMetadataResult?[] agentResults,
        CancellationToken cancellationToken
    )
    {
        var count = Math.Min(agentNames.Count, agentResults.Length);
        for (var i = 0; i < count; i++)
        {
            var result = agentResults[i];
            if (result?.Artwork is not { Count: > 0 })
            {
                continue;
            }

            foreach (var kvp in result.Artwork)
            {
                var ingested = await this
                    .imageService.IngestExternalArtworkAsync(
                        item,
                        agentNames[i],
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

    /// <inheritdoc />
    public async Task<bool> IngestArtworkAsync(
        MetadataItem item,
        MetadataBaseItem? metadata,
        string sourceIdentifier,
        CancellationToken cancellationToken
    )
    {
        if (metadata is null)
        {
            return false;
        }

        var changed = false;

        async Task<string?> ComputeHashAsync(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            return await this
                .imageService.ComputeThumbHashAsync(uri, cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<string?> IngestAsync(string? uri, ArtworkKind kind)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return null;
            }

            return await this
                .imageService.IngestExternalArtworkAsync(
                    item,
                    sourceIdentifier,
                    kind,
                    uri,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        var thumb = await IngestAsync(metadata.ThumbUri, ArtworkKind.Poster).ConfigureAwait(false);
        if (metadata.ThumbUri != thumb)
        {
            metadata.ThumbUri = thumb;
            changed = true;
        }

        var thumbHash = await ComputeHashAsync(metadata.ThumbUri).ConfigureAwait(false);
        if (metadata.ThumbHash != thumbHash)
        {
            metadata.ThumbHash = thumbHash;
            changed = true;
        }

        var backdrop = await IngestAsync(metadata.ArtUri, ArtworkKind.Backdrop)
            .ConfigureAwait(false);
        if (metadata.ArtUri != backdrop)
        {
            metadata.ArtUri = backdrop;
            changed = true;
        }

        var artHash = await ComputeHashAsync(metadata.ArtUri).ConfigureAwait(false);
        if (metadata.ArtHash != artHash)
        {
            metadata.ArtHash = artHash;
            changed = true;
        }

        var logo = await IngestAsync(metadata.LogoUri, ArtworkKind.Logo).ConfigureAwait(false);
        if (metadata.LogoUri != logo)
        {
            metadata.LogoUri = logo;
            changed = true;
        }

        var logoHash = await ComputeHashAsync(metadata.LogoUri).ConfigureAwait(false);
        if (metadata.LogoHash != logoHash)
        {
            metadata.LogoHash = logoHash;
            changed = true;
        }

        return changed;
    }

    /// <inheritdoc />
    public async Task IngestCreditArtworkAsync(
        IEnumerable<PersonCredit>? people,
        IEnumerable<GroupCredit>? groups,
        string sourceIdentifier,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(sourceIdentifier))
        {
            sourceIdentifier = "sidecar";
        }

        var seen = new HashSet<Guid>();

        async Task IngestForMetadataAsync(MetadataBaseItem metadata)
        {
            if (metadata.Uuid == Guid.Empty)
            {
                metadata.Uuid = Guid.NewGuid();
            }

            if (!seen.Add(metadata.Uuid))
            {
                return;
            }

            var owner = new MetadataItem { Uuid = metadata.Uuid };
            _ = await this.IngestArtworkAsync(owner, metadata, sourceIdentifier, cancellationToken)
                .ConfigureAwait(false);
        }

        if (people != null)
        {
            foreach (var person in people)
            {
                await IngestForMetadataAsync(person.Person).ConfigureAwait(false);
            }
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                await IngestForMetadataAsync(group.Group).ConfigureAwait(false);

                if (group.Members is { Count: > 0 })
                {
                    foreach (var member in group.Members)
                    {
                        await IngestForMetadataAsync(member.Person).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    /// <inheritdoc />
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

        // Reuse the internal helper for image generation.
        var providerNames = await this.RunImageProvidersAsync(meta, CancellationToken.None)
            .ConfigureAwait(false);

        // Select and persist artwork including image provider names in precedence list.
        await this.SelectAndPersistArtworkAsync(meta, providerNames, CancellationToken.None)
            .ConfigureAwait(false);

        this.LogGenerateImagesCompleted(metadataItemUuid);

        // Only enqueue trickplay generation for items with video media files.
        if (HasVideoMediaParts(meta))
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

    /// <inheritdoc />
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

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No media items found for metadata item {MetadataItemUuid}"
    )]
    private partial void LogNoMediaItemsFound(Guid metadataItemUuid);

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
        Message = "Running image provider {ProviderName} for media item {MediaItemId}"
    )]
    private partial void LogRunningImageProvider(string providerName, int mediaItemId);

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
        Message = "Generated images for {Count} media items for metadata item {MetadataItemUuid}"
    )]
    private partial void LogImagesGenerated(int count, Guid metadataItemUuid);

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
        Level = LogLevel.Warning,
        Message = "Metadata item {MetadataItemUuid} not found"
    )]
    private partial void LogMetadataItemNotFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting image generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateImagesStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed image generation for metadata item {MetadataItemUuid}"
    )]
    private partial void LogGenerateImagesCompleted(Guid metadataItemUuid);

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
    #endregion

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
