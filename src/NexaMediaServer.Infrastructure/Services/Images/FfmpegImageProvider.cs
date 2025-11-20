// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using FFMpegCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Image provider using FFmpeg to extract thumbnails from media files.
/// </summary>
/// <remarks>
/// For videos, this use the GoP (Group of Pictures) XML generated during analysis to select optimal frames.
/// For audio files, this extracts embedded album art if available.
/// </remarks>
public partial class FfmpegImageProvider : IImageProvider
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    private readonly ILogger<FfmpegImageProvider> logger;
    private readonly IGopIndexService gopIndexService;
    private readonly IImageService imageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="gopIndexService">The GoP index service.</param>
    /// <param name="imageService">The image service.</param>
    public FfmpegImageProvider(
        ILogger<FfmpegImageProvider> logger,
        IGopIndexService gopIndexService,
        IImageService imageService
    )
    {
        this.logger = logger;
        this.gopIndexService = gopIndexService;
        this.imageService = imageService;
    }

    /// <inheritdoc />
    public string Name => "FFmpeg Thumbnail Extractor";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public async Task ProvideAsync(
        MediaItem item,
        MetadataItem metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();
        this.LogProvideStart(item.Id, metadata.Id, parts?.Count ?? 0);

        // Basic guards
        if (parts == null || parts.Count == 0)
        {
            this.LogNoPartsForThumbnail(item.Id);
            this.LogProvideSkipped(item.Id, "no parts");
            return;
        }

        var firstPart = parts[0];
        if (string.IsNullOrWhiteSpace(firstPart.File) || !File.Exists(firstPart.File))
        {
            this.LogMissingPartFile(item.Id, firstPart.File ?? "<null>");
            this.LogProvideSkipped(item.Id, "missing part file");
            return;
        }

        // Only process video files for now (audio album art extraction could be added separately)
        var ext = Path.GetExtension(firstPart.File);
        if (!MediaFileExtensions.IsVideo(ext))
        {
            this.LogSkipNonVideo(item.Id, ext);
            this.LogProvideSkipped(item.Id, "not video");
            return;
        }

        await this.GenerateVideoThumbnailAsync(item, metadata, firstPart, cancellationToken)
            .ConfigureAwait(false);
        this.LogProvideComplete(item.Id);
    }

    /// <inheritdoc />
    public bool Supports(MediaItem item)
    {
        if (item == null)
        {
            return false;
        }

        var first = item.Parts?.FirstOrDefault();
        if (first == null)
        {
            this.LogNotSupportedNoParts(item.Id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(first.File))
        {
            this.LogNotSupportedBlankPath(item.Id);
            return false;
        }

        var ext = Path.GetExtension(first.File);
        var isVideo = MediaFileExtensions.IsVideo(ext);
        var isAudio = MediaFileExtensions.IsAudio(ext);
        var supported = isVideo || isAudio;
        this.LogSupportEvaluation(item.Id, ext, isVideo, isAudio, supported);
        return supported;
    }

    /// <summary>
    /// Resolves the total duration in milliseconds from available sources.
    /// </summary>
    /// <param name="gop">The GoP index if available.</param>
    /// <param name="item">The media item.</param>
    /// <param name="part">The media part.</param>
    /// <param name="metadata">The metadata item.</param>
    /// <returns>Total duration in milliseconds, or a default value if unavailable.</returns>
    private static long ResolveDurationMs(
        Core.DTOs.GopIndex? gop,
        MediaItem item,
        MediaPart part,
        MetadataItem metadata
    )
    {
        if (gop?.Groups.Count > 0)
        {
            var last = gop.Groups[^1];
            return last.PtsMs + Math.Max(0, last.DurationMs);
        }

        if (item.Duration.HasValue)
        {
            return (long)item.Duration.Value.TotalMilliseconds;
        }

        if (part.Duration.HasValue)
        {
            return (long)(part.Duration.Value * 1000.0);
        }

        if (metadata.Duration.HasValue)
        {
            return metadata.Duration.Value * 1000L;
        }

        // Unknown duration; default to 100 seconds for target calculation
        return 100_000;
    }

    /// <summary>
    /// Generates a temporary file path for thumbnail extraction.
    /// </summary>
    /// <param name="uuid">The metadata UUID.</param>
    /// <param name="ptsMs">The presentation timestamp in milliseconds.</param>
    /// <returns>The full path to the temporary file.</returns>
    private static string GetTempThumbnailPath(Guid uuid, long ptsMs)
    {
        var tempDir = Path.GetTempPath();
        var fileName = string.Create(
            null,
            stackalloc char[128],
            $"nexa_thumb_{uuid:N}_{ptsMs}.jpg"
        );
        return Path.Combine(tempDir, fileName);
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe not supported: no parts for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedNoParts(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe not supported: blank part path for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedBlankPath(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe support eval media item {MediaItemId}: ext={Extension} video={IsVideo} audio={IsAudio} supported={Supported}"
    )]
    private partial void LogSupportEvaluation(
        int mediaItemId,
        string? extension,
        bool isVideo,
        bool isAudio,
        bool supported
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: media item {MediaItemId} has no parts."
    )]
    private partial void LogNoPartsForThumbnail(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: part file missing for media item {MediaItemId} path={Path}"
    )]
    private partial void LogMissingPartFile(int mediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: non-video media item {MediaItemId} ext={Extension}"
    )]
    private partial void LogSkipNonVideo(int mediaItemId, string? Extension);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Selected GoP group for thumbnail: item={MediaItemId} ptsMs={PtsMs} durationMs={DurationMs}"
    )]
    private partial void LogSelectedGopGroup(int MediaItemId, long PtsMs, long DurationMs);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generated frame snapshot: item={MediaItemId} ptsMs={PtsMs} tempPath={TempPath}"
    )]
    private partial void LogGeneratedFrame(int MediaItemId, long PtsMs, string TempPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved thumbnail for metadata {MetadataId} via FFmpeg provider uri={Uri}"
    )]
    private partial void LogThumbnailSaved(int MetadataId, string Uri);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to generate thumbnail after retries for item={MediaItemId}"
    )]
    private partial void LogThumbnailGenerationFailed(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Fallback thumbnail using duration percent for item={MediaItemId} at {Ms}ms"
    )]
    private partial void LogFallbackNoGop(int MediaItemId, long Ms);

    [LoggerMessage(Level = LogLevel.Debug, Message = "FFmpeg snapshot fallback failed")]
    private partial void LogSnapshotFallbackFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to persist thumbnail for media item {MediaItemId}"
    )]
    private partial void LogPersistThumbnailFailed(Exception ex, int MediaItemId);

    private async Task GenerateVideoThumbnailAsync(
        MediaItem item,
        MetadataItem metadata,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gop = await this
            .gopIndexService.TryReadForPartAsync(item, part, cancellationToken)
            .ConfigureAwait(false);

        long totalDurationMs = ResolveDurationMs(gop, item, part, metadata);
        long targetMs = (long)(totalDurationMs * 0.10); // 10% mark
        this.LogGenerateVideoStart(
            item.Id,
            part.File,
            gop?.Groups.Count ?? 0,
            totalDurationMs,
            targetMs
        );
        string? acceptedTempPath = null;
        long acceptedPtsMs = -1;

        if (gop?.Groups.Count > 0)
        {
            // Select the single keyframe (GoP group) nearest to the 10% mark.
            var nearestGroup = gop.Groups.MinBy(g => Math.Abs(g.PtsMs - targetMs))!;
            this.LogSelectedGopGroup(item.Id, nearestGroup.PtsMs, nearestGroup.DurationMs);
            var tempPath = GetTempThumbnailPath(metadata.Uuid, nearestGroup.PtsMs);
            try
            {
                await FFMpeg
                    .SnapshotAsync(
                        part.File,
                        tempPath,
                        null,
                        TimeSpan.FromMilliseconds(nearestGroup.PtsMs),
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
                if (File.Exists(tempPath))
                {
                    this.LogGeneratedFrame(item.Id, nearestGroup.PtsMs, tempPath);
                    acceptedTempPath = tempPath;
                    acceptedPtsMs = nearestGroup.PtsMs;
                }
            }
            catch (Exception ex)
            {
                this.LogSnapshotFallbackFailed(ex);
            }
        }
        else
        {
            // No GoP index: snapshot at fixed 10 second mark.
            long fallbackMs = 10_000;
            this.LogFallbackNoGop(item.Id, fallbackMs);
            var tempPath = GetTempThumbnailPath(metadata.Uuid, fallbackMs);
            try
            {
                await FFMpeg
                    .SnapshotAsync(
                        part.File,
                        tempPath,
                        null,
                        TimeSpan.FromMilliseconds(fallbackMs),
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
                if (File.Exists(tempPath))
                {
                    acceptedTempPath = tempPath;
                    acceptedPtsMs = fallbackMs;
                }
            }
            catch (Exception ex)
            {
                this.LogSnapshotFallbackFailed(ex);
            }
        }

        if (acceptedTempPath == null)
        {
            this.LogThumbnailGenerationFailed(item.Id);
            return;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(acceptedTempPath, cancellationToken)
                .ConfigureAwait(false);
            var thumbUri = await this.imageService.SaveThumbnailAsync(
                metadata,
                this.Name,
                bytes,
                "jpg",
                cancellationToken
            );
            // Set ThumbUri immediately if not already populated (will be finalized by primary artwork selection).
            metadata.ThumbUri ??= thumbUri;
            this.LogThumbnailSaved(metadata.Id, thumbUri);
            this.LogThumbnailSuccessSummary(item.Id, acceptedPtsMs, thumbUri);
        }
        catch (Exception ex)
        {
            this.LogPersistThumbnailFailed(ex, item.Id);
        }
        finally
        {
            try
            {
                if (acceptedTempPath != null && File.Exists(acceptedTempPath))
                {
                    File.Delete(acceptedTempPath);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
    #endregion

    #region Additional High-Level Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFmpeg provider start: mediaItem={MediaItemId} metadata={MetadataId} parts={PartCount}"
    )]
    private partial void LogProvideStart(int MediaItemId, int MetadataId, int PartCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFmpeg provider skipped: mediaItem={MediaItemId} reason={Reason}"
    )]
    private partial void LogProvideSkipped(int MediaItemId, string Reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFmpeg provider complete: mediaItem={MediaItemId}"
    )]
    private partial void LogProvideComplete(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GenerateVideo start: mediaItem={MediaItemId} path={Path} gopGroups={GopGroups} durationMs={DurationMs} targetMs={TargetMs}"
    )]
    private partial void LogGenerateVideoStart(
        int MediaItemId,
        string Path,
        int GopGroups,
        long DurationMs,
        long TargetMs
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail success summary: mediaItem={MediaItemId} ptsMs={PtsMs} thumbUri={ThumbUri}"
    )]
    private partial void LogThumbnailSuccessSummary(int MediaItemId, long PtsMs, string ThumbUri);
    #endregion
}
