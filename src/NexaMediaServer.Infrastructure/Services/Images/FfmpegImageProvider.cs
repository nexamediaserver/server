// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using FFMpegCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Image provider using FFmpeg to extract thumbnails from media files.
/// </summary>
/// <remarks>
/// For videos, this extracts a frame at approximately 10% of the video duration.
/// For audio files, this extracts embedded album art if available.
/// </remarks>
public partial class FfmpegImageProvider : IImageProvider<Video>
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    private readonly ILogger<FfmpegImageProvider> logger;
    private readonly IImageService imageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="imageService">The image service.</param>
    public FfmpegImageProvider(ILogger<FfmpegImageProvider> logger, IImageService imageService)
    {
        this.logger = logger;
        this.imageService = imageService;
    }

    /// <inheritdoc />
    public string Name => "FFmpeg Thumbnail Extractor";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    bool IImageProvider<Video>.Supports(MediaItem item, Video metadata) =>
        this.SupportsInternal(item);

    /// <inheritdoc />
    Task IImageProvider<Video>.ProvideAsync(
        MediaItem item,
        Video metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.ProvideInternalAsync(item, metadata, parts, cancellationToken);

    /// <summary>
    /// Resolves the total duration in milliseconds from available sources.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <param name="part">The media part.</param>
    /// <param name="metadata">The metadata item.</param>
    /// <returns>Total duration in milliseconds, or a default value if unavailable.</returns>
    private static long ResolveDurationMs(MediaItem item, MediaPart part, MetadataBaseItem metadata)
    {
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

    private async Task ProvideInternalAsync(
        MediaItem item,
        Video metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();
        this.LogProvideStart(item.Id, metadata.Uuid, parts?.Count ?? 0);

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

    private bool SupportsInternal(MediaItem item)
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
        Message = "Generated frame snapshot: item={MediaItemId} ptsMs={PtsMs} tempPath={TempPath}"
    )]
    private partial void LogGeneratedFrame(int MediaItemId, long PtsMs, string TempPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved thumbnail for metadata {MetadataId} via FFmpeg provider uri={Uri}"
    )]
    private partial void LogThumbnailSaved(int MetadataId, string Uri);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved thumbnail for metadata {MetadataUuid} via FFmpeg provider uri={Uri}"
    )]
    private partial void LogThumbnailSaved(Guid MetadataUuid, string Uri);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to generate thumbnail after retries for item={MediaItemId}"
    )]
    private partial void LogThumbnailGenerationFailed(int MediaItemId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "FFmpeg snapshot failed")]
    private partial void LogSnapshotFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to persist thumbnail for media item {MediaItemId}"
    )]
    private partial void LogPersistThumbnailFailed(Exception ex, int MediaItemId);

    private async Task GenerateVideoThumbnailAsync(
        MediaItem item,
        MetadataBaseItem metadata,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        long totalDurationMs = ResolveDurationMs(item, part, metadata);
        long targetMs = (long)(totalDurationMs * 0.10); // 10% mark
        this.LogGenerateVideoStart(item.Id, part.File, totalDurationMs, targetMs);

        var tempPath = GetTempThumbnailPath(metadata.Uuid, targetMs);
        try
        {
            await FFMpeg
                .SnapshotAsync(
                    part.File,
                    tempPath,
                    null,
                    TimeSpan.FromMilliseconds(targetMs),
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            if (!File.Exists(tempPath))
            {
                this.LogThumbnailGenerationFailed(item.Id);
                return;
            }

            this.LogGeneratedFrame(item.Id, targetMs, tempPath);

            var bytes = await File.ReadAllBytesAsync(tempPath, cancellationToken)
                .ConfigureAwait(false);
            var thumbUri = await this.imageService.SaveThumbnailAsync(
                metadata.Uuid,
                this.Name,
                bytes,
                "jpg",
                cancellationToken
            );
            // Set ThumbUri immediately if not already populated (will be finalized by primary artwork selection).
            metadata.ThumbUri ??= thumbUri;
            this.LogThumbnailSaved(metadata.Uuid, thumbUri);
            this.LogThumbnailSuccessSummary(item.Id, targetMs, thumbUri);
        }
        catch (Exception ex)
        {
            this.LogSnapshotFailed(ex);
            this.LogThumbnailGenerationFailed(item.Id);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
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
        Message = "FFmpeg provider start: mediaItem={MediaItemId} metadata={MetadataUuid} parts={PartCount}"
    )]
    private partial void LogProvideStart(int MediaItemId, Guid MetadataUuid, int PartCount);

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
        Message = "GenerateVideo start: mediaItem={MediaItemId} path={Path} durationMs={DurationMs} targetMs={TargetMs}"
    )]
    private partial void LogGenerateVideoStart(
        int MediaItemId,
        string Path,
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
