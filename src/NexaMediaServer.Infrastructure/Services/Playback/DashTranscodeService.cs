// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
public sealed class DashTranscodeService : IDashTranscodeService
{
    private static readonly Action<ILogger, int, string, Exception?> LogDashGenerating =
        LoggerMessage.Define<int, string>(
            LogLevel.Information,
            new EventId(1, "DashGenerate"),
            "Generating DASH manifest for media part {MediaPartId} at {OutputDir}"
        );

    private readonly IFfmpegCommandBuilder commandBuilder;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IGopIndexService gopIndexService;
    private readonly ILogger<DashTranscodeService> logger;
    private readonly IApplicationPaths paths;
    private readonly TranscodeOptions transcodeOptions;

    /// <summary>
    /// Bounded lock pool with LRU eviction to prevent memory leaks from unbounded semaphore accumulation.
    /// </summary>
    private readonly BoundedLockPool lockPool = new(maxCapacity: 256);

    /// <summary>
    /// Initializes a new instance of the <see cref="DashTranscodeService"/> class.
    /// </summary>
    /// <param name="commandBuilder">FFmpeg command executor.</param>
    /// <param name="dbContextFactory">Factory for creating media contexts.</param>
    /// <param name="gopIndexService">GoP index reader.</param>
    /// <param name="paths">Application path provider.</param>
    /// <param name="transcodeOptions">Transcode settings.</param>
    /// <param name="logger">Logger instance.</param>
    public DashTranscodeService(
        IFfmpegCommandBuilder commandBuilder,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IGopIndexService gopIndexService,
        IApplicationPaths paths,
        IOptions<TranscodeOptions> transcodeOptions,
        ILogger<DashTranscodeService> logger
    )
    {
        this.commandBuilder = commandBuilder;
        this.dbContextFactory = dbContextFactory;
        this.gopIndexService = gopIndexService;
        this.logger = logger;
        this.paths = paths;
        this.transcodeOptions = transcodeOptions.Value;
    }

    /// <inheritdoc />
    public async Task<DashTranscodeResult> EnsureDashAsync(
        int mediaPartId,
        CancellationToken cancellationToken
    )
    {
        await using var lockHandle = await this
            .lockPool.AcquireAsync(mediaPartId, cancellationToken)
            .ConfigureAwait(false);
        try
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
            this.paths.EnsureDirectoryExists(outputDir);

            var manifestPath = Path.Combine(outputDir, "manifest.mpd");

            if (!File.Exists(manifestPath))
            {
                LogDashGenerating(this.logger, mediaPartId, outputDir, null);

                string? forceKeyFrames = await this.TryBuildKeyFrameExpressionAsync(
                        metadata.Uuid,
                        partIndex,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                var job = new DashTranscodeJob
                {
                    InputPath = part.File,
                    ManifestPath = manifestPath,
                    OutputDirectory = outputDir,
                    VideoCodec = this.transcodeOptions.DashVideoCodec,
                    AudioCodec = this.transcodeOptions.DashAudioCodec,
                    SegmentSeconds = this.transcodeOptions.DashSegmentDurationSeconds,
                    CopyVideo = false,
                    CopyAudio = false,
                    UseHardwareAcceleration =
                        this.transcodeOptions.HardwareAcceleration != HardwareAccelerationKind.None,
                    HardwareAcceleration = this.transcodeOptions.HardwareAcceleration,
                    EnableToneMapping = this.transcodeOptions.EnableToneMapping,
                    ForceKeyFramesExpression = forceKeyFrames,
                };

                await this
                    .commandBuilder.CreateDashAsync(job, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new DashTranscodeResult(manifestPath, outputDir);
        }
        finally
        {
            // Lock is released automatically via IAsyncDisposable
        }
    }

    /// <inheritdoc />
    public async Task<DashSeekResult> EnsureDashWithSeekAsync(
        int mediaPartId,
        long seekMs,
        CancellationToken cancellationToken
    )
    {
        // Use a composite key for locking: mediaPartId + seekMs bucket
        // This allows different seek positions to be transcoded independently
        var seekBucket = seekMs / 60000; // 1-minute buckets
        var lockKey = HashCode.Combine(mediaPartId, seekBucket);

        await using var lockHandle = await this
            .lockPool.AcquireAsync(lockKey, cancellationToken)
            .ConfigureAwait(false);

        try
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

            // Use a separate directory for seek-based transcodes to avoid conflicts
            // Include the seek position in the path so different seeks have different caches
            var seekBucketDir = (actualSeekMs / 60000).ToString(CultureInfo.InvariantCulture);
            var outputDir = Path.Combine(
                this.paths.CacheDirectory,
                "dash-seek",
                metadata.Uuid.ToString("N", CultureInfo.InvariantCulture),
                partIndex.ToString(CultureInfo.InvariantCulture),
                seekBucketDir
            );

            var manifestPath = Path.Combine(outputDir, "manifest.mpd");

            // Check if we already have a valid transcode for this position
            if (!File.Exists(manifestPath))
            {
                // Clean up the directory if it exists (partial transcode)
                if (System.IO.Directory.Exists(outputDir))
                {
                    System.IO.Directory.Delete(outputDir, recursive: true);
                }

                this.paths.EnsureDirectoryExists(outputDir);

                LogDashGenerating(this.logger, mediaPartId, outputDir, null);

                string? forceKeyFrames = await this.TryBuildKeyFrameExpressionAsync(
                        metadata.Uuid,
                        partIndex,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                var job = new DashTranscodeJob
                {
                    InputPath = part.File,
                    ManifestPath = manifestPath,
                    OutputDirectory = outputDir,
                    VideoCodec = this.transcodeOptions.DashVideoCodec,
                    AudioCodec = this.transcodeOptions.DashAudioCodec,
                    SegmentSeconds = this.transcodeOptions.DashSegmentDurationSeconds,
                    CopyVideo = false,
                    CopyAudio = false,
                    UseHardwareAcceleration =
                        this.transcodeOptions.HardwareAcceleration != HardwareAccelerationKind.None,
                    HardwareAcceleration = this.transcodeOptions.HardwareAcceleration,
                    EnableToneMapping = this.transcodeOptions.EnableToneMapping,
                    ForceKeyFramesExpression = forceKeyFrames,
                };

                await this
                    .commandBuilder.CreateDashWithSeekAsync(job, actualSeekMs, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new DashSeekResult(manifestPath, outputDir, actualSeekMs);
        }
        finally
        {
            // Lock is released automatically via IAsyncDisposable
        }
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

    /// <summary>
    /// A bounded pool of keyed locks with automatic cleanup to prevent memory leaks.
    /// Uses LRU eviction when the pool exceeds capacity.
    /// </summary>
    private sealed class BoundedLockPool
    {
        private readonly int maxCapacity;
        private readonly object syncRoot = new();
        private readonly Dictionary<int, LockEntry> locks = [];
        private readonly LinkedList<int> lruOrder = new();

        public BoundedLockPool(int maxCapacity)
        {
            this.maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Acquires an exclusive lock for the specified key.
        /// Returns a disposable handle that releases the lock when disposed.
        /// </summary>
        public async Task<IAsyncDisposable> AcquireAsync(
            int key,
            CancellationToken cancellationToken
        )
        {
            LockEntry entry;
            lock (this.syncRoot)
            {
                if (!this.locks.TryGetValue(key, out entry!))
                {
                    // Evict oldest entries if at capacity
                    while (this.locks.Count >= this.maxCapacity && this.lruOrder.Count > 0)
                    {
                        var oldest = this.lruOrder.First!.Value;
                        if (
                            this.locks.TryGetValue(oldest, out var oldEntry)
                            && oldEntry.ReferenceCount == 0
                        )
                        {
                            this.locks.Remove(oldest);
                            this.lruOrder.RemoveFirst();
                            oldEntry.Semaphore.Dispose();
                        }
                        else
                        {
                            // Entry is in use, move to end and try next
                            this.lruOrder.RemoveFirst();
                            if (oldEntry != null)
                            {
                                this.lruOrder.AddLast(oldest);
                            }

                            break;
                        }
                    }

                    entry = new LockEntry(new SemaphoreSlim(1, 1));
                    this.locks[key] = entry;
                    this.lruOrder.AddLast(key);
                }

                entry.ReferenceCount++;

                // Move to end of LRU list (most recently used)
                var node = this.lruOrder.Find(key);
                if (node != null)
                {
                    this.lruOrder.Remove(node);
                    this.lruOrder.AddLast(key);
                }
            }

            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new LockHandle(this, entry);
        }

        private void Release(LockEntry entry)
        {
            entry.Semaphore.Release();
            lock (this.syncRoot)
            {
                entry.ReferenceCount--;
            }
        }

        /// <summary>
        /// Handle that releases the lock when disposed.
        /// </summary>
        private readonly struct LockHandle(BoundedLockPool pool, LockEntry entry) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                pool.Release(entry);
                return ValueTask.CompletedTask;
            }
        }

        private sealed class LockEntry(SemaphoreSlim semaphore)
        {
            public SemaphoreSlim Semaphore { get; } = semaphore;
            public int ReferenceCount { get; set; }
        }
    }
}
