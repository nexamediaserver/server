// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Analyzes media files using FFProbe to extract technical metadata.
/// </summary>
public partial class FFProbeFileAnalyzer : IFileAnalyzer
{
    private readonly ILogger<FFProbeFileAnalyzer> logger;
    private readonly IApplicationPaths paths;
    private readonly IGopIndexService gopIndexService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFProbeFileAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="paths">Application paths provider.</param>
    /// <param name="gopIndexService">Service responsible for GoP index read/write operations.</param>
    public FFProbeFileAnalyzer(
        ILogger<FFProbeFileAnalyzer> logger,
        IApplicationPaths paths,
        IGopIndexService gopIndexService
    )
    {
        this.logger = logger;
        this.paths = paths;
        this.gopIndexService = gopIndexService;
    }

    /// <inheritdoc />
    public string Name => "FFProbe File Analyzer";

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public async Task<FileAnalysisResult?> AnalyzeAsync(
        MediaItem item,
        MetadataItem metadata,
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
            var currentPartIndex = partIndex; // Capture for closure

            if (string.IsNullOrWhiteSpace(file))
            {
                this.LogBlankPath(item.Id);
                continue;
            }

            processingTasks.Add(
                this.AnalyzePartAsync(item, metadata, currentPartIndex, file, cancellationToken)
            );
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
        Message = "Wrote GOP XML for media item {MediaItemId} file '{FilePath}' groups={GroupCount} path={OutputPath}"
    )]
    private partial void LogGopWritten(
        int mediaItemId,
        string filePath,
        int groupCount,
        string outputPath
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed writing GOP XML for media item {MediaItemId} file '{FilePath}'"
    )]
    private partial void LogGopWriteFailed(int mediaItemId, string filePath, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting GOP XML build for media item {MediaItemId} part {PartIndex} file '{FilePath}'"
    )]
    private partial void LogGopBuildStart(int mediaItemId, int partIndex, string filePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP path info for media item {MediaItemId}: uuid={Uuid} shard={Shard} baseDir={BaseDir} output={OutputPath}"
    )]
    private partial void LogGopPaths(
        int mediaItemId,
        string uuid,
        string shard,
        string baseDir,
        string outputPath
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Packet summary for media item {MediaItemId} file '{FilePath}': total={TotalPackets} videoFiltered={VideoPackets} streamIndex={StreamIndex}"
    )]
    private partial void LogGopPacketsSummary(
        int mediaItemId,
        string filePath,
        int totalPackets,
        int videoPackets,
        int streamIndex
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No video packets found for media item {MediaItemId} file '{FilePath}', skipping GOP XML"
    )]
    private partial void LogGopNoVideoPackets(int mediaItemId, string filePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP group {GroupIndex}: pts={PtsMs}ms duration={DurationMs}ms size={SizeBytes}B for media item {MediaItemId} file '{FilePath}'"
    )]
    private partial void LogGopGroup(
        int mediaItemId,
        string filePath,
        int groupIndex,
        long ptsMs,
        long durationMs,
        long sizeBytes
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping GOP XML: metadata item missing for media item {MediaItemId}"
    )]
    private partial void LogGopNoMetadata(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping GOP XML: invalid UUID '{Uuid}' for media item {MediaItemId}"
    )]
    private partial void LogGopInvalidUuid(int mediaItemId, string uuid);

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
        MetadataItem metadata,
        int partIndex,
        string file,
        CancellationToken cancellationToken
    )
    {
        try
        {
            this.LogProbingFile(item.Id, file);

            // Get file size asynchronously
            var fileInfo = new FileInfo(file);
            long fileSize = 0;
            await Task.Run(() => fileSize = fileInfo.Length, cancellationToken)
                .ConfigureAwait(false);

            // Perform FFProbe analysis once
            var mediaInfo = await FFProbe
                .AnalyseAsync(file, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

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

            // Build GOP index if video is present
            if (mediaInfo.VideoStreams.Count > 0)
            {
                try
                {
                    await this.BuildAndWriteGopIndexAsync(
                            item,
                            metadata,
                            partIndex,
                            file,
                            mediaInfo,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Non-fatal; continue overall analysis
                    this.LogGopWriteFailed(item.Id, file, ex);
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
    }

    private async Task BuildAndWriteGopIndexAsync(
        MediaItem item,
        MetadataItem metadata,
        int partIndex,
        string filePath,
        IMediaAnalysis mediaInfo,
        CancellationToken cancellationToken
    )
    {
        this.LogGopBuildStart(item.Id, partIndex, filePath);
        if (metadata == null)
        {
            this.LogGopNoMetadata(item.Id);
            return;
        }

        var uuid = metadata.Uuid.ToString("N");
        if (uuid.Length < 2)
        {
            this.LogGopInvalidUuid(item.Id, uuid);
            return;
        }

        var gopPath = this.gopIndexService.GetGopPath(metadata, partIndex);
        var shard = uuid[..2];
        var baseDir = Path.Combine(this.paths.MediaDirectory, shard, uuid);
        this.LogGopPaths(item.Id, uuid, shard, baseDir, gopPath);

        var packetAnalysis = await FFProbe
            .GetPacketsAsync(filePath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var packets = packetAnalysis.Packets;
        var primaryVideoStreamIndex =
            mediaInfo.PrimaryVideoStream?.Index
            ?? mediaInfo.VideoStreams.FirstOrDefault()?.Index ?? -1;

        // Filter video packets - FFProbe already returns packets in order, no need to sort
        var videoPackets = packets
            .Where(p =>
                string.Equals(p.CodecType, "video", StringComparison.OrdinalIgnoreCase)
                && (primaryVideoStreamIndex < 0 || p.StreamIndex == primaryVideoStreamIndex)
            )
            .ToList();

        this.LogGopPacketsSummary(
            item.Id,
            filePath,
            packets.Count,
            videoPackets.Count,
            primaryVideoStreamIndex
        );

        if (videoPackets.Count == 0)
        {
            this.LogGopNoVideoPackets(item.Id, filePath);
            return;
        }

        static TimeSpan ParseTime(string s)
        {
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
            {
                return ts;
            }

            return TimeSpan.Zero;
        }

        // Pre-parse all timestamps once to avoid repeated parsing
        var parsedPackets = new List<ParsedPacket>(videoPackets.Count);
        foreach (var pkt in videoPackets)
        {
            parsedPackets.Add(
                new ParsedPacket
                {
                    OriginalPacket = pkt,
                    Pts = ParseTime(pkt.PtsTime),
                    Duration = ParseTime(pkt.DurationTime),
                    IsKeyFrame = pkt.Flags.StartsWith("K", StringComparison.OrdinalIgnoreCase),
                }
            );
        }

        var index = new Core.DTOs.GopIndex();
        var currentPackets = new List<ParsedPacket>();
        int currentKeyIndex = -1;

        // Process packets and build GOP groups using indexed iteration to avoid O(n²)
        for (int i = 0; i < parsedPackets.Count; i++)
        {
            var parsed = parsedPackets[i];

            if (parsed.IsKeyFrame)
            {
                // Finalize previous group if exists
                if (currentKeyIndex >= 0 && currentPackets.Count > 0)
                {
                    this.FinalizeGroup(currentPackets, parsedPackets, i, item.Id, filePath, index);
                }

                currentPackets.Clear();
                currentKeyIndex = i;
                currentPackets.Add(parsed);
            }
            else if (currentKeyIndex >= 0)
            {
                currentPackets.Add(parsed);
            }
        }

        // Finalize the last group
        if (currentKeyIndex >= 0 && currentPackets.Count > 0)
        {
            this.FinalizeGroup(currentPackets, parsedPackets, -1, item.Id, filePath, index);
        }

        if (index.Groups.Count == 0)
        {
            return;
        }

        await this
            .gopIndexService.WriteAsync(metadata, partIndex, index, cancellationToken)
            .ConfigureAwait(false);
        this.LogGopWritten(item.Id, filePath, index.Groups.Count, gopPath);
    }

    private void FinalizeGroup(
        List<ParsedPacket> groupPackets,
        List<ParsedPacket> allParsedPackets,
        int nextKeyIndex,
        int mediaItemId,
        string filePath,
        Core.DTOs.GopIndex index
    )
    {
        var keyPacket = groupPackets[0];
        var startTime = keyPacket.Pts;
        var last = groupPackets[^1];

        TimeSpan endTime;
        if (nextKeyIndex >= 0)
        {
            // Next keyframe exists, use its PTS as end time
            endTime = allParsedPackets[nextKeyIndex].Pts;
        }
        else
        {
            // Last group: use last packet's PTS + duration
            endTime = last.Pts + last.Duration;
            if (endTime < last.Pts)
            {
                endTime = last.Pts;
            }
        }

        var duration = endTime - startTime;
        var ptsMs = (long)Math.Round(startTime.TotalMilliseconds);
        var durationMs = (long)Math.Round(duration.TotalMilliseconds);

        // Fallback: sum individual packet durations if calculated duration is invalid
        if (durationMs <= 0)
        {
            var sum = groupPackets.Sum(p => p.Duration.TotalMilliseconds);
            if (sum > 0)
            {
                durationMs = (long)Math.Round(sum);
            }
        }

        var size = groupPackets.Sum(p => (long)p.OriginalPacket.Size);
        var groupIndex = index.Groups.Count;
        index.Groups.Add(new Core.DTOs.GopGroup(ptsMs, durationMs, size));

        // Log sampling: only log every 10th group to reduce overhead
        if (groupIndex % 10 == 0 || groupIndex < 3)
        {
            this.LogGopGroup(mediaItemId, filePath, groupIndex, ptsMs, durationMs, size);
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

    private sealed class ParsedPacket
    {
        public required FFProbePacketAnalysis OriginalPacket { get; init; }

        public required TimeSpan Pts { get; init; }

        public required TimeSpan Duration { get; init; }

        public required bool IsKeyFrame { get; init; }
    }
}
