// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Parts;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Service responsible for orchestrating file analysis (FFprobe, MediaInfo)
/// and merging technical metadata into media items.
/// </summary>
public sealed partial class FileAnalysisOrchestrator : IFileAnalysisOrchestrator
{
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly IPartsRegistry partsRegistry;
    private readonly ILogger<FileAnalysisOrchestrator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAnalysisOrchestrator"/> class.
    /// </summary>
    /// <param name="metadataItemRepository">The metadata item repository.</param>
    /// <param name="partsRegistry">Registry providing typed file analyzers.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    public FileAnalysisOrchestrator(
        IMetadataItemRepository metadataItemRepository,
        IPartsRegistry partsRegistry,
        ILogger<FileAnalysisOrchestrator> logger
    )
    {
        this.metadataItemRepository = metadataItemRepository;
        this.partsRegistry = partsRegistry;
        this.logger = logger;
    }

    /// <inheritdoc />
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
                MergeContributions(media, contributions);
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

    private static void MergeContributions(MediaItem media, List<FileAnalysisResult> contributions)
    {
        long? fileSize = contributions.Select(c => c.FileSizeBytes).LastOrDefault(c => c.HasValue);
        string? fileFormat = contributions.Select(c => c.FileFormat).LastOrDefault(f => f != null);
        TimeSpan? duration = contributions.Select(c => c.Duration).LastOrDefault(d => d.HasValue);
        bool? hasChapters = contributions.Select(c => c.HasChapters).LastOrDefault(h => h.HasValue);
        int? chapterCount = contributions
            .Select(c => c.ChapterCount)
            .LastOrDefault(h => h.HasValue);
        bool? isDisc = contributions.Select(c => c.IsDisc).LastOrDefault(h => h.HasValue);
        string? discTitle = contributions.Select(c => c.DiscTitle).LastOrDefault(f => f != null);
        string? discId = contributions.Select(c => c.DiscId).LastOrDefault(f => f != null);
        string? videoCodec = contributions.Select(c => c.VideoCodec).LastOrDefault(f => f != null);
        string? videoCodecProfile = contributions
            .Select(c => c.VideoCodecProfile)
            .LastOrDefault(f => f != null);
        string? videoCodecLevel = contributions
            .Select(c => c.VideoCodecLevel)
            .LastOrDefault(f => f != null);
        int? videoBitrate = contributions
            .Select(c => c.VideoBitrate)
            .LastOrDefault(v => v.HasValue);
        int? videoWidth = contributions.Select(c => c.VideoWidth).LastOrDefault(v => v.HasValue);
        int? videoHeight = contributions.Select(c => c.VideoHeight).LastOrDefault(v => v.HasValue);
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
        static void MergeCollection<T>(ICollection<T> target, IEnumerable<ICollection<T>?> source)
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
        MergeCollection(media.AudioChannelCounts, contributions.Select(c => c.AudioChannelCounts));
        MergeCollection(media.AudioSampleRates, contributions.Select(c => c.AudioSampleRates));
        MergeCollection(media.AudioBitDepths, contributions.Select(c => c.AudioBitDepths));
        MergeCollection(media.AudioBitrates, contributions.Select(c => c.AudioBitrates));
        media.AudioTrackCount =
            contributions.Select(c => c.AudioTrackCount).LastOrDefault(v => v.HasValue)
            ?? media.AudioTrackCount;
        media.SubtitleTrackCount =
            contributions.Select(c => c.SubtitleTrackCount).LastOrDefault(v => v.HasValue)
            ?? media.SubtitleTrackCount;
        MergeCollection(media.SubtitleLanguages, contributions.Select(c => c.SubtitleLanguages));
        MergeCollection(media.SubtitleFormats, contributions.Select(c => c.SubtitleFormats));
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

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting file analysis for metadata item {MetadataItemUuid}"
    )]
    private partial void LogAnalyzeFilesStarted(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Metadata item {MetadataItemUuid} not found for file analysis"
    )]
    private partial void LogMetadataItemNotFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No media items found for metadata item {MetadataItemUuid}"
    )]
    private partial void LogNoMediaItemsFound(Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {MediaItemCount} media items for metadata item {MetadataItemUuid}"
    )]
    private partial void LogMediaItemsFound(int mediaItemCount, Guid metadataItemUuid);

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

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analyzed {Count} media items for metadata item {MetadataItemUuid}"
    )]
    private partial void LogMediaItemsAnalyzed(int count, Guid metadataItemUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed file analysis for metadata item {MetadataItemUuid}"
    )]
    private partial void LogAnalyzeFilesCompleted(Guid metadataItemUuid);
    #endregion

}
