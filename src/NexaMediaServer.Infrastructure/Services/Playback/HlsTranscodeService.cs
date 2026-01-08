// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text;

using AsyncKeyedLock;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Coordinates HLS manifest and segment generation using ffmpeg.
/// </summary>
public sealed partial class HlsTranscodeService : IHlsTranscodeService, IDisposable
{
    /// <summary>
    /// Default timeout for waiting on segment files to be created during transcoding.
    /// </summary>
    private static readonly TimeSpan SegmentWaitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Polling interval for checking segment file existence.
    /// </summary>
    private static readonly TimeSpan SegmentPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly IFfmpegCommandBuilder commandBuilder;
    private readonly IFfmpegCapabilities capabilities;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IGopIndexService gopIndexService;
    private readonly ITranscodeJobCache jobCache;
    private readonly ILogger<HlsTranscodeService> logger;
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
    /// Initializes a new instance of the <see cref="HlsTranscodeService"/> class.
    /// </summary>
    /// <param name="commandBuilder">FFmpeg command executor.</param>
    /// <param name="capabilities">FFmpeg capabilities.</param>
    /// <param name="dbContextFactory">Factory for creating media contexts.</param>
    /// <param name="gopIndexService">GoP index reader.</param>
    /// <param name="jobCache">In-memory transcode job cache.</param>
    /// <param name="paths">Application path provider.</param>
    /// <param name="transcodeOptions">Transcode settings.</param>
    /// <param name="logger">Logger instance.</param>
    public HlsTranscodeService(
        IFfmpegCommandBuilder commandBuilder,
        IFfmpegCapabilities capabilities,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IGopIndexService gopIndexService,
        ITranscodeJobCache jobCache,
        IApplicationPaths paths,
        IOptionsMonitor<TranscodeOptions> transcodeOptions,
        ILogger<HlsTranscodeService> logger
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
    public async Task<HlsTranscodeResult> EnsureHlsAsync(
        int mediaPartId,
        AbrLadder? abrLadder,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken)
            .ConfigureAwait(false);

        if (part == null)
        {
            throw new InvalidOperationException(
                $"Media part {mediaPartId} not found for HLS transcode."
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
            "hls",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture)
        );

        var masterPlaylistPath = Path.Combine(outputDir, "master.m3u8");

        // Determine variants to generate
        var variants = abrLadder?.Variants ?? [CreateDefaultVariant(part.MediaItem)];

        // Fast path: master playlist already exists, check if all variants exist
        if (File.Exists(masterPlaylistPath))
        {
            var variantPaths = variants.ToDictionary(
                v => v.Id,
                v => Path.Combine(outputDir, v.Id, "playlist.m3u8"));

            if (variantPaths.Values.All(File.Exists))
            {
                return new HlsTranscodeResult(masterPlaylistPath, outputDir, variantPaths);
            }
        }

        // Lock on the output directory path to prevent concurrent transcodes
        using (await this.transcodeLocks.LockAsync(outputDir, cancellationToken).ConfigureAwait(false))
        {
            // Kill any existing transcode for this path before starting a new one
            await this.jobCache.KillByPathAsync(outputDir, cancellationToken).ConfigureAwait(false);

            this.paths.EnsureDirectoryExists(outputDir);

            var variantPaths = new Dictionary<string, string>();

            // Generate master playlist if it doesn't exist
            if (!File.Exists(masterPlaylistPath))
            {
                this.LogHlsGenerating(mediaPartId, outputDir);
                await GenerateMasterPlaylistAsync(
                    masterPlaylistPath,
                    variants,
                    cancellationToken
                );
            }

            // Ensure each variant is transcoded
            foreach (var variant in variants)
            {
                var variantDir = Path.Combine(outputDir, variant.Id);
                var variantPlaylistPath = Path.Combine(variantDir, "playlist.m3u8");
                variantPaths[variant.Id] = variantPlaylistPath;

                if (!File.Exists(variantPlaylistPath))
                {
                    this.paths.EnsureDirectoryExists(variantDir);
                    await this.TranscodeVariantAsync(
                        part,
                        metadata.Uuid,
                        partIndex,
                        variant,
                        variantDir,
                        seekMs: null,
                        cancellationToken
                    );
                }
            }

            return new HlsTranscodeResult(masterPlaylistPath, outputDir, variantPaths);
        }
    }

    /// <inheritdoc />
    public async Task<string> EnsureVariantAsync(
        int mediaPartId,
        AbrVariant variant,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken)
            .ConfigureAwait(false);

        if (part?.MediaItem?.MetadataItem == null)
        {
            throw new InvalidOperationException($"Media part {mediaPartId} not found.");
        }

        var metadata = part.MediaItem.MetadataItem;
        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken);

        var outputDir = Path.Combine(
            this.paths.CacheDirectory,
            "hls",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture),
            variant.Id
        );

        var playlistPath = Path.Combine(outputDir, "playlist.m3u8");

        // Fast path: playlist already exists
        if (File.Exists(playlistPath))
        {
            return playlistPath;
        }

        // Lock on the output directory path
        using (await this.transcodeLocks.LockAsync(outputDir, cancellationToken).ConfigureAwait(false))
        {
            // Double-check after acquiring lock
            if (File.Exists(playlistPath))
            {
                return playlistPath;
            }

            // Kill any existing transcode for this path
            await this.jobCache.KillByPathAsync(outputDir, cancellationToken).ConfigureAwait(false);

            this.paths.EnsureDirectoryExists(outputDir);
            await this.TranscodeVariantAsync(
                part,
                metadata.Uuid,
                partIndex,
                variant,
                outputDir,
                null,
                cancellationToken
            );
        }

        return playlistPath;
    }

    /// <inheritdoc />
    public async Task<HlsSeekResult> EnsureHlsWithSeekAsync(
        int mediaPartId,
        long seekMs,
        AbrLadder? abrLadder,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken)
            .ConfigureAwait(false);

        if (part?.MediaItem?.MetadataItem == null)
        {
            throw new InvalidOperationException($"Media part {mediaPartId} not found.");
        }

        var metadata = part.MediaItem.MetadataItem;
        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken);

        // Find the nearest keyframe
        long actualSeekMs = await this.FindNearestKeyframeAsync(
            metadata.Uuid,
            partIndex,
            seekMs,
            cancellationToken
        );

        var seekBucketDir = (actualSeekMs / 60000).ToString(CultureInfo.InvariantCulture);
        var outputDir = Path.Combine(
            this.paths.CacheDirectory,
            "hls-seek",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture),
            seekBucketDir
        );

        var masterPlaylistPath = Path.Combine(outputDir, "master.m3u8");
        var variants = abrLadder?.Variants ?? [CreateDefaultVariant(part.MediaItem)];

        // Fast path: check if all files exist
        if (File.Exists(masterPlaylistPath))
        {
            var variantPaths = variants.ToDictionary(
                v => v.Id,
                v => Path.Combine(outputDir, v.Id, "playlist.m3u8"));

            if (variantPaths.Values.All(File.Exists))
            {
                return new HlsSeekResult(masterPlaylistPath, outputDir, actualSeekMs, variantPaths);
            }
        }

        // Lock on the output directory path
        using (await this.transcodeLocks.LockAsync(outputDir, cancellationToken).ConfigureAwait(false))
        {
            var variantPaths = variants.ToDictionary(
                v => v.Id,
                v => Path.Combine(outputDir, v.Id, "playlist.m3u8"));

            // Double-check after acquiring lock
            if (File.Exists(masterPlaylistPath) && variantPaths.Values.All(File.Exists))
            {
                return new HlsSeekResult(masterPlaylistPath, outputDir, actualSeekMs, variantPaths);
            }

            // Kill any existing transcode for this path
            await this.jobCache.KillByPathAsync(outputDir, cancellationToken).ConfigureAwait(false);

            // Clean up partial transcode
            if (System.IO.Directory.Exists(outputDir))
            {
                System.IO.Directory.Delete(outputDir, recursive: true);
            }

            this.paths.EnsureDirectoryExists(outputDir);
            this.LogHlsGenerating(mediaPartId, outputDir);

            await GenerateMasterPlaylistAsync(
                masterPlaylistPath,
                variants,
                cancellationToken
            );

            foreach (var variant in variants)
            {
                var variantDir = Path.Combine(outputDir, variant.Id);
                var variantPlaylistPath = Path.Combine(variantDir, "playlist.m3u8");
                variantPaths[variant.Id] = variantPlaylistPath;

                this.paths.EnsureDirectoryExists(variantDir);
                await this.TranscodeVariantAsync(
                    part,
                    metadata.Uuid,
                    partIndex,
                    variant,
                    variantDir,
                    actualSeekMs,
                    cancellationToken
                );
            }

            return new HlsSeekResult(masterPlaylistPath, outputDir, actualSeekMs, variantPaths);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetSegmentPathAsync(
        int mediaPartId,
        string variantId,
        int segmentNumber,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var part = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken);

        if (part?.MediaItem?.MetadataItem == null)
        {
            return null;
        }

        var metadata = part.MediaItem.MetadataItem;
        int partIndex = await ResolvePartIndexAsync(db, part, cancellationToken);

        // Try normal HLS directory first
        var segmentPath = Path.Combine(
            this.paths.CacheDirectory,
            "hls",
            metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
            partIndex.ToString(CultureInfo.InvariantCulture),
            variantId,
            $"segment{segmentNumber}.m4s"
        );

        if (File.Exists(segmentPath))
        {
            return segmentPath;
        }

        // Try MPEG-TS format
        segmentPath = Path.ChangeExtension(segmentPath, ".ts");
        return File.Exists(segmentPath) ? segmentPath : null;
    }

    private static async Task GenerateMasterPlaylistAsync(
        string masterPlaylistPath,
        IReadOnlyList<AbrVariant> variants,
        CancellationToken cancellationToken
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"#EXTM3U");
        sb.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-VERSION:7");

        foreach (var variant in variants.OrderByDescending(v => v.TotalBitrate))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-STREAM-INF:BANDWIDTH={variant.TotalBitrate},RESOLUTION={variant.Width}x{variant.Height},CODECS=\"avc1.640028,mp4a.40.2\"");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{variant.Id}/playlist.m3u8");
        }

        await File.WriteAllTextAsync(masterPlaylistPath, sb.ToString(), cancellationToken);
    }

    private static AbrVariant CreateDefaultVariant(MediaItem mediaItem)
    {
        return new AbrVariant
        {
            Id = "auto",
            Label = "Auto",
            Width = mediaItem.VideoWidth ?? 1920,
            Height = mediaItem.VideoHeight ?? 1080,
            VideoBitrate = mediaItem.VideoBitrate ?? 8_000_000,
            AudioBitrate = 192_000,
            AudioChannels = 2,
            IsSource = true,
        };
    }

    private static bool CanCopyVideo(MediaItem mediaItem)
    {
        // Can only copy if codec is compatible with HLS (h264/h265)
        var codec = mediaItem.VideoCodec?.ToLowerInvariant();
        return codec is "h264" or "hevc" or "h265";
    }

    private static bool CanCopyAudio(MediaItem mediaItem)
    {
        // Can only copy if codec is compatible with HLS (aac/mp3)
        var codec = mediaItem.AudioCodecs.FirstOrDefault()?.ToLowerInvariant();
        return codec is "aac" or "mp3";
    }

    private static async Task<int> ResolvePartIndexAsync(
        MediaServerContext db,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        var partIds = await db.MediaParts
            .Where(p => p.MediaItemId == part.MediaItemId)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

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

    private async Task TranscodeVariantAsync(
        MediaPart part,
        Guid metadataUuid,
        int partIndex,
        AbrVariant variant,
        string variantDir,
        long? seekMs,
        CancellationToken cancellationToken
    )
    {
        bool hasVideo = !string.IsNullOrWhiteSpace(part.MediaItem.VideoCodec);
        bool hasAudio =
            part.MediaItem.AudioTrackCount.GetValueOrDefault() > 0
            || part.MediaItem.AudioCodecs.Count > 0;

        string? forceKeyFrames = await this.TryBuildKeyFrameExpressionAsync(
            metadataUuid,
            partIndex,
            cancellationToken
        );

        // Extract video stream properties for filter context
        var mediaItem = part.MediaItem;
        var options = this.transcodeOptions.CurrentValue;
        var sourceCodec = mediaItem.VideoCodec ?? "h264";
        var hwAccel = options.EffectiveAcceleration;
        var copyVideo = variant.IsSource && CanCopyVideo(mediaItem);
        var useHwDecoder = !copyVideo &&
                          hwAccel != HardwareAccelerationKind.None &&
                          this.capabilities.IsHardwareDecoderAvailable(sourceCodec, hwAccel);

        var job = new HlsTranscodeJob
        {
            InputPath = part.File,
            MasterPlaylistPath = Path.Combine(Path.GetDirectoryName(variantDir)!, "master.m3u8"),
            OutputDirectory = variantDir,
            VariantId = variant.Id,
            VideoCodec = options.HlsVideoCodec,
            AudioCodec = options.HlsAudioCodec,
            SegmentSeconds = options.HlsSegmentDurationSeconds,
            CopyVideo = copyVideo,
            CopyAudio = variant.IsSource && CanCopyAudio(mediaItem),
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
            ForceKeyFramesExpression = forceKeyFrames,
            HasVideo = hasVideo,
            HasAudio = hasAudio,
            TargetWidth = variant.IsSource ? null : variant.Width,
            TargetHeight = variant.IsSource ? null : variant.Height,
            VideoBitrate = variant.VideoBitrate,
            AudioBitrate = variant.AudioBitrate,
            AudioChannels = variant.AudioChannels,
            UseFragmentedMp4 = true,
        };

        if (seekMs.HasValue)
        {
            await this.commandBuilder.CreateHlsWithSeekAsync(job, seekMs.Value, cancellationToken);
        }
        else
        {
            await this.commandBuilder.CreateHlsAsync(job, cancellationToken);
        }
    }

    private async Task<long> FindNearestKeyframeAsync(
        Guid metadataUuid,
        int partIndex,
        long seekMs,
        CancellationToken cancellationToken
    )
    {
        var gop = await this.gopIndexService
            .TryReadAsync(metadataUuid, partIndex, cancellationToken);

        if (gop == null || gop.Groups.Count == 0)
        {
            return seekMs;
        }

        var nearestKeyframe = gop.Groups
            .Where(g => g.PtsMs <= seekMs)
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
        var gop = await this.gopIndexService
            .TryReadAsync(metadataUuid, partIndex, cancellationToken);

        if (gop == null || gop.Groups.Count == 0)
        {
            return null;
        }

        var timestamps = gop.Groups
            .Select(g => g.PtsMs / 1000.0)
            .Where(t => t >= 0)
            .Select(t => t.ToString("0.###", CultureInfo.InvariantCulture))
            .ToList();

        return timestamps.Count == 0 ? null : string.Join(',', timestamps);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Generating HLS manifest for media part {MediaPartId} at {OutputDir}")]
    private partial void LogHlsGenerating(int mediaPartId, string outputDir);
}
