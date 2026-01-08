// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AsyncKeyedLock;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Coordinates DASH manifest generation using ffmpeg.
/// </summary>
public sealed class DashTranscodeService : IDashTranscodeService, IDisposable
{
    /// <summary>
    /// Default timeout for waiting on segment files to be created during transcoding.
    /// </summary>
    private static readonly TimeSpan SegmentWaitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Polling interval for checking segment file existence.
    /// </summary>
    private static readonly TimeSpan SegmentPollInterval = TimeSpan.FromMilliseconds(100);

    private static readonly Action<ILogger, int, string, Exception?> LogDashGenerating =
        LoggerMessage.Define<int, string>(
            LogLevel.Information,
            new EventId(1, "DashGenerate"),
            "Generating DASH manifest for media part {MediaPartId} at {OutputDir}"
        );

    private readonly IFfmpegCommandBuilder commandBuilder;
    private readonly IFfmpegCapabilities capabilities;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IGopIndexService gopIndexService;
    private readonly ITranscodeJobCache jobCache;
    private readonly ILogger<DashTranscodeService> logger;
    private readonly IApplicationPaths paths;
    private readonly IOptionsMonitor<TranscodeOptions> transcodeOptions;

    /// <summary>
    /// AsyncKeyedLocker for path-based locking, matching Jellyfin's configuration.
    /// Pool size of 20 with initial fill of 1.
    /// </summary>
    private readonly AsyncKeyedLocker<string> transcodeLocks = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });

    /// <summary>
    /// Initializes a new instance of the <see cref="DashTranscodeService"/> class.
    /// </summary>
    /// <param name="commandBuilder">FFmpeg command executor.</param>
    /// <param name="capabilities">FFmpeg capabilities.</param>
    /// <param name="dbContextFactory">Factory for creating media contexts.</param>
    /// <param name="gopIndexService">GoP index reader.</param>
    /// <param name="jobCache">In-memory transcode job cache.</param>
    /// <param name="paths">Application path provider.</param>
    /// <param name="transcodeOptions">Transcode settings.</param>
    /// <param name="logger">Logger instance.</param>
    public DashTranscodeService(
        IFfmpegCommandBuilder commandBuilder,
        IFfmpegCapabilities capabilities,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IGopIndexService gopIndexService,
        ITranscodeJobCache jobCache,
        IApplicationPaths paths,
        IOptionsMonitor<TranscodeOptions> transcodeOptions,
        ILogger<DashTranscodeService> logger
    )
    {
        this.commandBuilder = commandBuilder;
        this.capabilities = capabilities;
        this.dbContextFactory = dbContextFactory;
        this.gopIndexService = gopIndexService;
        this.jobCache = jobCache;
        this.logger = logger;
        this.paths = paths;
        this.transcodeOptions = transcodeOptions;
    }

    /// <inheritdoc />
    public void Dispose() => this.transcodeLocks.Dispose();

    /// <inheritdoc />
    public async Task<bool> WaitForSegmentAsync(string segmentPath, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(SegmentWaitTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!File.Exists(segmentPath))
            {
                await Task.Delay(SegmentPollInterval, linkedCts.Token).ConfigureAwait(false);
            }

            return true;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<DashTranscodeResult> EnsureDashAsync(
        int mediaPartId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );
        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken)
            .ConfigureAwait(false);

        if (part == null)
        {
            throw new InvalidOperationException(
                $"Media part {mediaPartId} not found for DASH transcode."
            );
        }

        var metadata = part.MediaItem.MetadataItem;
        if (metadata == null)
        {
            throw new InvalidOperationException(
                $"Media part {mediaPartId} is missing metadata."
            );
        }

        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken)
            .ConfigureAwait(false);
        var outputDir = Path.Combine(
            this.paths.CacheDirectory,
            "dash",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture)
        );

        var manifestPath = Path.Combine(outputDir, "manifest.mpd");

        // Fast path: manifest already exists, no lock needed
        if (File.Exists(manifestPath))
        {
            return new DashTranscodeResult(manifestPath, outputDir);
        }

        // Lock on the output directory path to prevent concurrent transcodes
        using (await this.transcodeLocks.LockAsync(outputDir, cancellationToken).ConfigureAwait(false))
        {
            // Double-check after acquiring lock - another request may have created the manifest
            if (File.Exists(manifestPath))
            {
                return new DashTranscodeResult(manifestPath, outputDir);
            }

            // Kill any existing transcode for this path before starting a new one
            await this.jobCache.KillByPathAsync(outputDir, cancellationToken).ConfigureAwait(false);

            this.paths.EnsureDirectoryExists(outputDir);

            LogDashGenerating(this.logger, mediaPartId, outputDir, null);

            bool hasVideo = !string.IsNullOrWhiteSpace(part.MediaItem.VideoCodec);
            bool hasAudio =
                part.MediaItem.AudioTrackCount.GetValueOrDefault() > 0
                || part.MediaItem.AudioCodecs.Count > 0;

            string? forceKeyFrames = await this.TryBuildKeyFrameExpressionAsync(
                    metadata.Uuid,
                    partIndex,
                    cancellationToken
                )
                .ConfigureAwait(false);

            // Extract video stream properties for filter context
            var mediaItem = part.MediaItem;
            var options = this.transcodeOptions.CurrentValue;
            var sourceCodec = mediaItem.VideoCodec ?? "h264";
            var hwAccel = options.EffectiveAcceleration;
            var useHwDecoder = hwAccel != HardwareAccelerationKind.None &&
                               this.capabilities.IsHardwareDecoderAvailable(sourceCodec, hwAccel);

            var job = new DashTranscodeJob
            {
                InputPath = part.File,
                ManifestPath = manifestPath,
                OutputDirectory = outputDir,
                VideoCodec = options.DashVideoCodec,
                AudioCodec = options.DashAudioCodec,
                SegmentSeconds = options.DashSegmentDurationSeconds,
                CopyVideo = false,
                CopyAudio = false,
                UseHardwareAcceleration =
                        hwAccel != HardwareAccelerationKind.None,
                HardwareAcceleration = hwAccel,
                EnableToneMapping = options.EnableToneMapping,
                IsHdr = IsHdrContent(mediaItem.VideoDynamicRange),
                IsInterlaced = mediaItem.VideoIsInterlaced ?? false,
                Rotation = 0, // Video rotation is handled per-stream, not per-item
                SourceVideoCodec = sourceCodec,
                UseHardwareDecoder = useHwDecoder,
                SourceWidth = mediaItem.VideoWidth ?? 1920,
                SourceHeight = mediaItem.VideoHeight ?? 1080,
                TargetWidth = null, // DASH uses source resolution by default
                TargetHeight = null,
                ForceKeyFramesExpression = forceKeyFrames,
                HasVideo = hasVideo,
                HasAudio = hasAudio,
            };

            await this
                .commandBuilder.CreateDashAsync(job, cancellationToken)
                .ConfigureAwait(false);
        }

        return new DashTranscodeResult(manifestPath, outputDir);
    }

    /// <inheritdoc />
    public async Task<DashSeekResult> EnsureDashWithSeekAsync(
        int mediaPartId,
        long seekMs,
        int? startSegmentNumber,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );
        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken)
            .ConfigureAwait(false);

        if (part == null)
        {
            throw new InvalidOperationException(
                $"Media part {mediaPartId} not found for DASH transcode."
            );
        }

        var metadata = part.MediaItem.MetadataItem;
        if (metadata == null)
        {
            throw new InvalidOperationException(
                $"Media part {mediaPartId} is missing metadata."
            );
        }

        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken)
            .ConfigureAwait(false);

        // Find the nearest keyframe to seek to
        long actualSeekMs = await this.FindNearestKeyframeAsync(
                metadata.Uuid,
                partIndex,
                seekMs,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Use the SAME directory as EnsureDashAsync - this allows segment requests
        // to continue working against the same directory after a seek restart
        var outputDir = Path.Combine(
            this.paths.CacheDirectory,
            "dash",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture)
        );

        var manifestPath = Path.Combine(outputDir, "manifest.mpd");

        // Lock on the output directory path to prevent concurrent transcodes
        using (await this.transcodeLocks.LockAsync(outputDir, cancellationToken).ConfigureAwait(false))
        {
            // Kill any existing transcode for this path before starting a new one
            await this.jobCache.KillByPathAsync(outputDir, cancellationToken).ConfigureAwait(false);

            // Clean up existing segments (but keep init segments if possible)
            // This is necessary because segment numbers will change after the seek
            if (System.IO.Directory.Exists(outputDir))
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(
                             outputDir,
                             "chunk-*.m4s",
                             SearchOption.AllDirectories))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // Ignore file deletion errors
                    }
                }

                // Also delete the manifest so it gets regenerated
                if (System.IO.File.Exists(manifestPath))
                {
                    try
                    {
                        System.IO.File.Delete(manifestPath);
                    }
                    catch (IOException)
                    {
                        // Ignore
                    }
                }
            }

            this.paths.EnsureDirectoryExists(outputDir);

            LogDashGenerating(this.logger, mediaPartId, outputDir, null);

            bool hasVideo = !string.IsNullOrWhiteSpace(part.MediaItem.VideoCodec);
            bool hasAudio =
                part.MediaItem.AudioTrackCount.GetValueOrDefault() > 0
                || part.MediaItem.AudioCodecs.Count > 0;

            string? forceKeyFrames = await this.TryBuildKeyFrameExpressionAsync(
                    metadata.Uuid,
                    partIndex,
                    cancellationToken
                )
                .ConfigureAwait(false);

            // Extract video stream properties for filter context
            var mediaItem = part.MediaItem;
            var options = this.transcodeOptions.CurrentValue;
            var sourceCodec = mediaItem.VideoCodec ?? "h264";
            var hwAccel = options.EffectiveAcceleration;
            var useHwDecoder = hwAccel != HardwareAccelerationKind.None &&
                               this.capabilities.IsHardwareDecoderAvailable(sourceCodec, hwAccel);

            // Calculate starting segment number based on client request when available to avoid off-by-one
            // mismatch between DASH Number%05d template and player expectations after a seek.
            var segmentDurationMs = options.DashSegmentDurationSeconds * 1000;
            var effectiveStartNumber = startSegmentNumber ?? Math.Max(1, (int)(seekMs / segmentDurationMs) + 1);

            var job = new DashTranscodeJob
            {
                InputPath = part.File,
                ManifestPath = manifestPath,
                OutputDirectory = outputDir,
                VideoCodec = options.DashVideoCodec,
                AudioCodec = options.DashAudioCodec,
                SegmentSeconds = options.DashSegmentDurationSeconds,
                CopyVideo = false,
                CopyAudio = false,
                UseHardwareAcceleration =
                    hwAccel != HardwareAccelerationKind.None,
                HardwareAcceleration = hwAccel,
                EnableToneMapping = options.EnableToneMapping,
                IsHdr = IsHdrContent(mediaItem.VideoDynamicRange),
                IsInterlaced = mediaItem.VideoIsInterlaced ?? false,
                Rotation = 0, // Video rotation is handled per-stream, not per-item
                SourceVideoCodec = sourceCodec,
                UseHardwareDecoder = useHwDecoder,
                SourceWidth = mediaItem.VideoWidth ?? 1920,
                SourceHeight = mediaItem.VideoHeight ?? 1080,
                TargetWidth = null,
                TargetHeight = null,
                ForceKeyFramesExpression = forceKeyFrames,
                HasVideo = hasVideo,
                HasAudio = hasAudio,
                StartSegmentNumber = effectiveStartNumber,
            };

            await this
                .commandBuilder.CreateDashWithSeekAsync(job, actualSeekMs, cancellationToken)
                .ConfigureAwait(false);
        }

        return new DashSeekResult(manifestPath, outputDir, actualSeekMs);
    }

    private static async Task<int> ResolvePartIndexAsync(
        MediaServerContext db,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        var partIds = await db
            .MediaParts.Where(p => p.MediaItemId == part.MediaItemId)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var index = partIds.FindIndex(id => id == part.Id);
        return index < 0 ? 0 : index;
    }

    /// <summary>
    /// Determines if the dynamic range indicates HDR content.
    /// </summary>
    /// <param name="dynamicRange">The dynamic range string (e.g., "HDR10", "DolbyVision", "HLG").</param>
    /// <returns>True if the content is HDR; otherwise, false.</returns>
    private static bool IsHdrContent(string? dynamicRange)
    {
        if (string.IsNullOrWhiteSpace(dynamicRange))
        {
            return false;
        }

        return dynamicRange.Contains("HDR", StringComparison.OrdinalIgnoreCase) ||
               dynamicRange.Contains("Dolby", StringComparison.OrdinalIgnoreCase) ||
               dynamicRange.Contains("HLG", StringComparison.OrdinalIgnoreCase) ||
               dynamicRange.Contains("PQ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Finds the nearest keyframe position to the requested seek time.
    /// </summary>
    private async Task<long> FindNearestKeyframeAsync(
        Guid metadataUuid,
        int partIndex,
        long seekMs,
        CancellationToken cancellationToken
    )
    {
        var gop = await this
            .gopIndexService.TryReadAsync(metadataUuid, partIndex, cancellationToken)
            .ConfigureAwait(false);

        if (gop == null || gop.Groups.Count == 0)
        {
            // No GoP index available, use the requested position
            return seekMs;
        }

        // Find the keyframe at or before the seek position
        var nearestKeyframe = gop
            .Groups.Where(g => g.PtsMs <= seekMs)
            .Select(g => g.PtsMs)
            .LastOrDefault();

        return nearestKeyframe;
    }

    private async Task<string?> TryBuildKeyFrameExpressionAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    )
    {
        var gop = await this
            .gopIndexService.TryReadAsync(metadataUuid, partIndex, cancellationToken)
            .ConfigureAwait(false);

        if (gop == null || gop.Groups.Count == 0)
        {
            return null;
        }

        var timestamps = gop
            .Groups.Select(g => g.PtsMs / 1000.0)
            .Where(t => t >= 0)
            .Select(t => t.ToString("0.###", CultureInfo.InvariantCulture))
            .ToList();
        if (timestamps.Count == 0)
        {
            return null;
        }

        return string.Join(',', timestamps);
    }
}
