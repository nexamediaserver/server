// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Analyzes media files using FFProbe to extract technical metadata.
/// </summary>
public partial class FFProbeFileAnalyzer : IFileAnalyzer<Audio>, IFileAnalyzer<Video>
{
    /// <summary>
    /// Maximum concurrent FFProbe operations to prevent I/O contention.
    /// </summary>
    private const int MaxConcurrentProbes = 4;

    private static readonly SemaphoreSlim ProbeSemaphore = new(
        MaxConcurrentProbes,
        MaxConcurrentProbes
    );

    private readonly ILogger<FFProbeFileAnalyzer> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFProbeFileAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FFProbeFileAnalyzer(ILogger<FFProbeFileAnalyzer> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public string Name => "FFProbe File Analyzer";

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    bool IFileAnalyzer<Video>.Supports(MediaItem item, Video metadata) =>
        this.SupportsInternal(item);

    /// <inheritdoc />
    bool IFileAnalyzer<Audio>.Supports(MediaItem item, Audio metadata) =>
        this.SupportsInternal(item);

    /// <inheritdoc />
    Task<FileAnalysisResult?> IFileAnalyzer<Video>.AnalyzeAsync(
        MediaItem item,
        Video metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.AnalyzeInternalAsync(item, parts, cancellationToken);

    /// <inheritdoc />
    Task<FileAnalysisResult?> IFileAnalyzer<Audio>.AnalyzeAsync(
        MediaItem item,
        Audio metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.AnalyzeInternalAsync(item, parts, cancellationToken);

    private async Task<FileAnalysisResult?> AnalyzeInternalAsync(
        MediaItem item,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    )
    {
        if (parts == null || parts.Count == 0)
        {
            this.LogNoParts(item.Id);
            return null;
        }

        long totalSize = 0;
        TimeSpan? duration = null;
        string? fileFormat = null;
        bool? hasChapters = null;
        int? chapterCount = null;
        var videoCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? videoCodecProfile = null;
        string? videoCodecLevel = null;
        int? videoBitrate = null;
        var audioCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var audioLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var audioChannelCounts = new HashSet<int>();
        var audioSampleRates = new HashSet<int>();
        var audioBitDepths = new HashSet<int>();
        var audioBitrates = new HashSet<int>();
        int? audioTrackCount = null;
        int? subtitleTrackCount = null;
        var subtitleLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var subtitleFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? videoDynamicRange = null;
        int? width = null;
        int? height = null;
        string? videoAspectRatio = null;
        double? frameRate = null;
        bool? videoIsInterlaced = null;
        int? videoBitDepth = null;
        string? videoColorSpace = null;
        string? videoColorPrimaries = null;
        string? videoColorTransfer = null;
        string? videoColorRange = null;

        // Process all parts in parallel for better performance
        var processingTasks = new List<Task<PartAnalysisResult?>>();
        for (var partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            var part = parts[partIndex];
            var file = part.File;

            if (string.IsNullOrWhiteSpace(file))
            {
                this.LogBlankPath(item.Id);
                continue;
            }

            processingTasks.Add(this.AnalyzePartAsync(item, file, cancellationToken));
        }

        var results = await Task.WhenAll(processingTasks).ConfigureAwait(false);

        // Aggregate results from all parts
        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            totalSize += result.FileSize;

            if (result.Duration > TimeSpan.Zero && (duration == null || result.Duration > duration))
            {
                duration = result.Duration;
            }

            // File format - take first non-null
            fileFormat ??= result.FileFormat;

            // Chapters - any part with chapters means the file has chapters
            if (result.HasChapters)
            {
                hasChapters = true;
                chapterCount = (chapterCount ?? 0) + result.ChapterCount;
            }

            // Video properties - take first non-null values
            if (result.VideoCodec != null)
            {
                videoCodecs.Add(result.VideoCodec);
            }

            videoCodecProfile ??= result.VideoCodecProfile;
            videoCodecLevel ??= result.VideoCodecLevel;
            videoBitrate ??= result.VideoBitrate;

            if (width == null && result.Width > 0)
            {
                width = result.Width;
            }

            if (height == null && result.Height > 0)
            {
                height = result.Height;
            }

            videoAspectRatio ??= result.VideoAspectRatio;

            if (frameRate == null && result.FrameRate > 0)
            {
                frameRate = result.FrameRate;
            }

            videoIsInterlaced ??= result.VideoIsInterlaced;
            videoBitDepth ??= result.VideoBitDepth;
            videoColorSpace ??= result.VideoColorSpace;
            videoColorPrimaries ??= result.VideoColorPrimaries;
            videoColorTransfer ??= result.VideoColorTransfer;
            videoColorRange ??= result.VideoColorRange;
            videoDynamicRange ??= result.VideoDynamicRange;

            // Audio properties - aggregate all
            if (result.AudioCodecs != null)
            {
                audioCodecs.UnionWith(result.AudioCodecs);
            }

            if (result.AudioLanguages != null)
            {
                audioLanguages.UnionWith(result.AudioLanguages);
            }

            if (result.AudioChannelCounts != null)
            {
                audioChannelCounts.UnionWith(result.AudioChannelCounts);
            }

            if (result.AudioSampleRates != null)
            {
                audioSampleRates.UnionWith(result.AudioSampleRates);
            }

            if (result.AudioBitDepths != null)
            {
                audioBitDepths.UnionWith(result.AudioBitDepths);
            }

            if (result.AudioBitrates != null)
            {
                audioBitrates.UnionWith(result.AudioBitrates);
            }

            audioTrackCount = (audioTrackCount ?? 0) + result.AudioTrackCount;

            // Subtitle properties - aggregate all
            subtitleTrackCount = (subtitleTrackCount ?? 0) + result.SubtitleTrackCount;

            if (result.SubtitleLanguages != null)
            {
                subtitleLanguages.UnionWith(result.SubtitleLanguages);
            }

            if (result.SubtitleFormats != null)
            {
                subtitleFormats.UnionWith(result.SubtitleFormats);
            }
        }

        if (totalSize == 0 && duration == null && videoCodecs.Count == 0 && audioCodecs.Count == 0)
        {
            this.LogNoData(item.Id);
            return null;
        }

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.LogExtracted(
                item.Id,
                totalSize,
                duration,
                videoCodecs.FirstOrDefault(),
                audioCodecs.Count,
                width,
                height,
                frameRate
            );
        }

        return new FileAnalysisResult
        {
            FileSizeBytes = totalSize > 0 ? totalSize : null,
            FileFormat = fileFormat,
            Duration = duration,
            HasChapters = hasChapters,
            ChapterCount = chapterCount > 0 ? chapterCount : null,
            VideoCodec = videoCodecs.FirstOrDefault(),
            VideoCodecProfile = videoCodecProfile,
            VideoCodecLevel = videoCodecLevel,
            VideoBitrate = videoBitrate,
            VideoWidth = width,
            VideoHeight = height,
            VideoAspectRatio = videoAspectRatio,
            VideoFrameRate = frameRate,
            VideoIsInterlaced = videoIsInterlaced,
            VideoBitDepth = videoBitDepth,
            VideoColorSpace = videoColorSpace,
            VideoColorPrimaries = videoColorPrimaries,
            VideoColorTransfer = videoColorTransfer,
            VideoColorRange = videoColorRange,
            VideoDynamicRange = videoDynamicRange,
            AudioCodecs = audioCodecs.Count > 0 ? audioCodecs.ToList() : null,
            AudioLanguages = audioLanguages.Count > 0 ? audioLanguages.ToList() : null,
            AudioChannelCounts = audioChannelCounts.Count > 0 ? audioChannelCounts.ToList() : null,
            AudioSampleRates = audioSampleRates.Count > 0 ? audioSampleRates.ToList() : null,
            AudioBitDepths = audioBitDepths.Count > 0 ? audioBitDepths.ToList() : null,
            AudioBitrates = audioBitrates.Count > 0 ? audioBitrates.ToList() : null,
            AudioTrackCount = audioTrackCount > 0 ? audioTrackCount : null,
            SubtitleTrackCount = subtitleTrackCount > 0 ? subtitleTrackCount : null,
            SubtitleLanguages = subtitleLanguages.Count > 0 ? subtitleLanguages.ToList() : null,
            SubtitleFormats = subtitleFormats.Count > 0 ? subtitleFormats.ToList() : null,
        };
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
        Message = "FFProbe analyzer skipped: no parts for media item {MediaItemId}"
    )]
    private partial void LogNoParts(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe analyzer skipping blank/whitespace file path for media item {MediaItemId}"
    )]
    private partial void LogBlankPath(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe probing file '{FilePath}' for media item {MediaItemId}"
    )]
    private partial void LogProbingFile(int mediaItemId, string filePath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "FFProbe failed for media item {MediaItemId} file '{FilePath}'"
    )]
    private partial void LogProbeFailed(int mediaItemId, string filePath, Exception? ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe produced no data for media item {MediaItemId}"
    )]
    private partial void LogNoData(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFProbe extracted size={Size} duration={Duration} vCodec={VideoCodec} aCodecCount={AudioCodecCount} width={Width} height={Height} fps={Fps} for media item {MediaItemId}"
    )]
    private partial void LogExtracted(
        int mediaItemId,
        long size,
        TimeSpan? duration,
        string? videoCodec,
        int audioCodecCount,
        int? width,
        int? height,
        double? fps
    );

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
    #endregion

    private async Task<PartAnalysisResult?> AnalyzePartAsync(
        MediaItem item,
        string file,
        CancellationToken cancellationToken
    )
    {
        // Throttle concurrent FFProbe operations to prevent I/O contention
        await ProbeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            this.LogProbingFile(item.Id, file);

            // Perform FFProbe analysis once
            var mediaInfo = await FFProbe
                .AnalyseAsync(file, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Calculate file size from bitrate and duration when available,
            // otherwise fall back to FileInfo. This avoids a redundant stat() call
            // when FFProbe already provides the data we need.
            long fileSize;
            var bitRate = mediaInfo.Format.BitRate;
            if (bitRate > 0 && mediaInfo.Duration > TimeSpan.Zero)
            {
                // Estimate size from bitrate: (bits/sec * seconds) / 8 = bytes
                fileSize = (long)(bitRate * mediaInfo.Duration.TotalSeconds / 8);
            }
            else
            {
                // Fall back to FileInfo only when bitrate unavailable
                var fileInfo = new FileInfo(file);
                fileSize = fileInfo.Exists ? fileInfo.Length : 0;
            }

            var partDuration = mediaInfo.Duration;

            // Extract file format
            var fileFormat = mediaInfo.Format.FormatName?.Split(',').FirstOrDefault();

            // Extract chapter information
            var hasChapters = mediaInfo.Chapters.Count > 0;
            var chapterCount = mediaInfo.Chapters.Count;

            // Extract video stream properties
            string? videoCodec = null;
            string? videoCodecProfile = null;
            string? videoCodecLevel = null;
            int? videoBitrate = null;
            int width = 0;
            int height = 0;
            string? videoAspectRatio = null;
            double frameRate = 0;
            bool? videoIsInterlaced = null;
            int? videoBitDepth = null;
            string? videoColorSpace = null;
            string? videoColorPrimaries = null;
            string? videoColorTransfer = null;
            string? videoColorRange = null;

            var primaryVideoStream = mediaInfo.PrimaryVideoStream;
            if (primaryVideoStream != null)
            {
                videoCodec = primaryVideoStream.CodecName;
                videoCodecProfile = !string.IsNullOrWhiteSpace(primaryVideoStream.Profile)
                    ? primaryVideoStream.Profile
                    : null;
                videoCodecLevel =
                    primaryVideoStream.Level > 0
                        ? primaryVideoStream.Level.ToString(CultureInfo.InvariantCulture)
                        : null;
                videoBitrate =
                    primaryVideoStream.BitRate > 0 ? (int)primaryVideoStream.BitRate : null;
                width = primaryVideoStream.Width;
                height = primaryVideoStream.Height;

                // Format aspect ratio as "width:height"
                if (
                    primaryVideoStream.DisplayAspectRatio.Width > 0
                    && primaryVideoStream.DisplayAspectRatio.Height > 0
                )
                {
                    videoAspectRatio =
                        $"{primaryVideoStream.DisplayAspectRatio.Width}:{primaryVideoStream.DisplayAspectRatio.Height}";
                }

                frameRate = primaryVideoStream.AvgFrameRate;
                videoBitDepth = primaryVideoStream.BitDepth;

                videoColorSpace = !string.IsNullOrWhiteSpace(primaryVideoStream.ColorSpace)
                    ? primaryVideoStream.ColorSpace
                    : null;
                videoColorPrimaries = !string.IsNullOrWhiteSpace(primaryVideoStream.ColorPrimaries)
                    ? primaryVideoStream.ColorPrimaries
                    : null;
                videoColorTransfer = !string.IsNullOrWhiteSpace(primaryVideoStream.ColorTransfer)
                    ? primaryVideoStream.ColorTransfer
                    : null;
                videoColorRange = !string.IsNullOrWhiteSpace(primaryVideoStream.ColorRange)
                    ? primaryVideoStream.ColorRange
                    : null;

                // Note: VideoIsInterlaced is not directly available from FFProbe
                // It would require parsing field_order or scanning_type from tags
            }

            // Detect dynamic range based on color transfer characteristics
            string? videoDynamicRange = null;
            if (
                primaryVideoStream != null
                && !string.IsNullOrWhiteSpace(primaryVideoStream.ColorTransfer)
            )
            {
                var transfer = primaryVideoStream.ColorTransfer.ToLowerInvariant();
                if (transfer.Contains("smpte2084") || transfer.Contains("pq"))
                {
                    videoDynamicRange = "HDR10";
                }
                else if (transfer.Contains("arib-std-b67") || transfer.Contains("hlg"))
                {
                    videoDynamicRange = "HLG";
                }
                else if (transfer.Contains("bt709") || transfer.Contains("bt.709"))
                {
                    videoDynamicRange = "SDR";
                }
            }

            // Extract audio stream properties
            var audioCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var audioLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var audioChannelCounts = new HashSet<int>();
            var audioSampleRates = new HashSet<int>();
            var audioBitDepths = new HashSet<int>();
            var audioBitrates = new HashSet<int>();

            foreach (var audio in mediaInfo.AudioStreams)
            {
                if (!string.IsNullOrWhiteSpace(audio.CodecName))
                {
                    audioCodecs.Add(audio.CodecName);
                }

                if (!string.IsNullOrWhiteSpace(audio.Language) && audio.Language != "und")
                {
                    audioLanguages.Add(audio.Language);
                }

                if (audio.Channels > 0)
                {
                    audioChannelCounts.Add(audio.Channels);
                }

                if (audio.SampleRateHz > 0)
                {
                    audioSampleRates.Add(audio.SampleRateHz);
                }

                if (audio.BitDepth.HasValue && audio.BitDepth.Value > 0)
                {
                    audioBitDepths.Add(audio.BitDepth.Value);
                }

                if (audio.BitRate > 0)
                {
                    audioBitrates.Add((int)audio.BitRate);
                }
            }

            // Extract subtitle stream properties
            var subtitleLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var subtitleFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var subtitle in mediaInfo.SubtitleStreams)
            {
                if (!string.IsNullOrWhiteSpace(subtitle.Language) && subtitle.Language != "und")
                {
                    subtitleLanguages.Add(subtitle.Language);
                }

                if (!string.IsNullOrWhiteSpace(subtitle.CodecName))
                {
                    subtitleFormats.Add(subtitle.CodecName);
                }
            }

            return new PartAnalysisResult(
                fileSize,
                partDuration,
                fileFormat,
                hasChapters,
                chapterCount,
                videoCodec,
                videoCodecProfile,
                videoCodecLevel,
                videoBitrate,
                width,
                height,
                videoAspectRatio,
                frameRate,
                videoIsInterlaced,
                videoBitDepth,
                videoColorSpace,
                videoColorPrimaries,
                videoColorTransfer,
                videoColorRange,
                videoDynamicRange,
                audioCodecs.Count > 0 ? audioCodecs : null,
                audioLanguages.Count > 0 ? audioLanguages : null,
                audioChannelCounts.Count > 0 ? audioChannelCounts : null,
                audioSampleRates.Count > 0 ? audioSampleRates : null,
                audioBitDepths.Count > 0 ? audioBitDepths : null,
                audioBitrates.Count > 0 ? audioBitrates : null,
                mediaInfo.AudioStreams.Count,
                mediaInfo.SubtitleStreams.Count,
                subtitleLanguages.Count > 0 ? subtitleLanguages : null,
                subtitleFormats.Count > 0 ? subtitleFormats : null
            );
        }
        catch (Exception ex)
        {
            // Ignore probe failures for individual parts
            this.LogProbeFailed(item.Id, file, ex);
            return null;
        }
        finally
        {
            ProbeSemaphore.Release();
        }
    }

    private sealed record PartAnalysisResult(
        long FileSize,
        TimeSpan Duration,
        string? FileFormat,
        bool HasChapters,
        int ChapterCount,
        string? VideoCodec,
        string? VideoCodecProfile,
        string? VideoCodecLevel,
        int? VideoBitrate,
        int Width,
        int Height,
        string? VideoAspectRatio,
        double FrameRate,
        bool? VideoIsInterlaced,
        int? VideoBitDepth,
        string? VideoColorSpace,
        string? VideoColorPrimaries,
        string? VideoColorTransfer,
        string? VideoColorRange,
        string? VideoDynamicRange,
        HashSet<string>? AudioCodecs,
        HashSet<string>? AudioLanguages,
        HashSet<int>? AudioChannelCounts,
        HashSet<int>? AudioSampleRates,
        HashSet<int>? AudioBitDepths,
        HashSet<int>? AudioBitrates,
        int AudioTrackCount,
        int SubtitleTrackCount,
        HashSet<string>? SubtitleLanguages,
        HashSet<string>? SubtitleFormats
    );
}
