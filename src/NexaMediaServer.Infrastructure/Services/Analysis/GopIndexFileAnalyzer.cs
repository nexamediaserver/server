// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Builds GOP (Group of Pictures) indexes for video files to enable efficient seeking.
/// This analyzer runs after FFProbeFileAnalyzer to build GOP indexes for video content.
/// </summary>
public partial class GopIndexFileAnalyzer : IFileAnalyzer<Video>
{
    private const string PacketProjectionArgs =
        "-show_entries packet=pts_time,duration_time,flags,size,stream_index,codec_type";
    private const string VideoPacketSelectionArgs = "-select_streams v";

    private readonly ILogger<GopIndexFileAnalyzer> logger;
    private readonly IApplicationPaths paths;
    private readonly IGopIndexService gopIndexService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GopIndexFileAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="paths">Application paths provider.</param>
    /// <param name="gopIndexService">Service responsible for GoP index read/write operations.</param>
    public GopIndexFileAnalyzer(
        ILogger<GopIndexFileAnalyzer> logger,
        IApplicationPaths paths,
        IGopIndexService gopIndexService
    )
    {
        this.logger = logger;
        this.paths = paths;
        this.gopIndexService = gopIndexService;
    }

    /// <inheritdoc />
    public string Name => "GOP Index Builder";

    /// <inheritdoc />
    /// <remarks>
    /// Order 100 ensures this runs after FFProbeFileAnalyzer (Order 0) so that
    /// basic file metadata is already available.
    /// </remarks>
    public int Order => 100;

    /// <inheritdoc />
    public bool Supports(MediaItem item, Video metadata)
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
        this.LogSupportEvaluation(item.Id, ext, isVideo);
        return isVideo;
    }

    /// <inheritdoc />
    public async Task<FileAnalysisResult?> AnalyzeAsync(
        MediaItem item,
        Video metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    )
    {
        if (parts == null || parts.Count == 0)
        {
            this.LogNoParts(item.Id);
            return null;
        }

        // Build GOP indexes for all parts with video content
        for (var partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            var part = parts[partIndex];
            var file = part.File;

            if (string.IsNullOrWhiteSpace(file))
            {
                this.LogBlankPath(item.Id);
                continue;
            }

            var ext = Path.GetExtension(file);
            if (!MediaFileExtensions.IsVideo(ext))
            {
                continue;
            }

            try
            {
                await this.BuildGopIndexAsync(item, metadata, partIndex, file, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Non-fatal; continue with other parts
                this.LogGopWriteFailed(item.Id, file, ex);
            }
        }

        // This analyzer doesn't contribute metadata - it only builds GOP indexes
        return null;
    }

    /// <summary>
    /// Parse time as seconds (double) directly - more efficient than TimeSpan.TryParse.
    /// </summary>
    private static TimeSpan ParseTime(string s)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.Zero;
    }

    private static void FinalizeGroup(
        List<ParsedPacket> groupPackets,
        TimeSpan? nextKeyPts,
        Core.DTOs.GopIndex index
    )
    {
        var keyPacket = groupPackets[0];
        var startTime = keyPacket.Pts;
        var last = groupPackets[^1];

        TimeSpan endTime;
        if (nextKeyPts.HasValue)
        {
            endTime = nextKeyPts.Value;
            if (endTime < startTime)
            {
                endTime = startTime;
            }
        }
        else
        {
            endTime = last.Pts + last.Duration;
            if (endTime < last.Pts)
            {
                endTime = last.Pts;
            }
        }

        var duration = endTime - startTime;
        var ptsMs = (long)Math.Round(startTime.TotalMilliseconds);
        var durationMs = (long)Math.Round(duration.TotalMilliseconds);

        if (durationMs <= 0)
        {
            var sum = groupPackets.Sum(p => p.Duration.TotalMilliseconds);
            if (sum > 0)
            {
                durationMs = (long)Math.Round(sum);
            }
        }

        var size = groupPackets.Sum(p => (long)p.OriginalPacket.Size);

        index.Groups.Add(new Core.DTOs.GopGroup(ptsMs, durationMs, size));
    }

    private async Task BuildGopIndexAsync(
        MediaItem item,
        Video metadata,
        int partIndex,
        string filePath,
        CancellationToken cancellationToken
    )
    {
        this.LogGopBuildStart(item.Id, partIndex, filePath);

        var uuid = metadata.Uuid.ToString("N");
        if (uuid.Length < 2)
        {
            this.LogGopInvalidUuid(item.Id, uuid);
            return;
        }

        // Skip if GOP index already exists (avoid redundant FFProbe packet analysis)
        var existingIndex = await this
            .gopIndexService.TryReadAsync(metadata.Uuid, partIndex, cancellationToken)
            .ConfigureAwait(false);
        if (existingIndex != null)
        {
            this.LogGopAlreadyExists(item.Id, partIndex);
            return;
        }

        var gopPath = this.gopIndexService.GetGopPath(metadata.Uuid, partIndex);
        var shard = uuid[..2];
        var baseDir = Path.Combine(this.paths.MediaDirectory, shard, uuid);
        this.LogGopPaths(item.Id, uuid, shard, baseDir, gopPath);

        // Get packet data from FFProbe
        var packetAnalysis = await FFProbe
            .GetPacketsAsync(
                filePath,
                cancellationToken: cancellationToken,
                customArguments: $"{VideoPacketSelectionArgs} {PacketProjectionArgs}"
            )
            .ConfigureAwait(false);

        var packets = packetAnalysis.Packets;

        // We need to determine the primary video stream index
        // Since we don't have IMediaAnalysis here, we'll use the first video packet's stream index
        int? primaryVideoStreamIndex = null;
        foreach (var pkt in packets)
        {
            if (string.Equals(pkt.CodecType, "video", StringComparison.OrdinalIgnoreCase))
            {
                primaryVideoStreamIndex = pkt.StreamIndex;
                break;
            }
        }

        if (!primaryVideoStreamIndex.HasValue)
        {
            this.LogGopNoVideoPackets(item.Id, filePath);
            return;
        }

        var index = new Core.DTOs.GopIndex();
        var currentPackets = new List<ParsedPacket>(capacity: 128);
        int videoPacketCount = 0;

        foreach (var pkt in packets)
        {
            if (!string.Equals(pkt.CodecType, "video", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pkt.StreamIndex != primaryVideoStreamIndex.Value)
            {
                continue;
            }

            videoPacketCount++;
            var parsed = new ParsedPacket
            {
                OriginalPacket = pkt,
                Pts = ParseTime(pkt.PtsTime),
                Duration = ParseTime(pkt.DurationTime),
                IsKeyFrame = pkt.Flags.StartsWith("K", StringComparison.OrdinalIgnoreCase),
            };

            if (parsed.IsKeyFrame)
            {
                if (currentPackets.Count > 0)
                {
                    FinalizeGroup(currentPackets, parsed.Pts, index);
                    currentPackets.Clear();
                }

                currentPackets.Add(parsed);
            }
            else if (currentPackets.Count > 0)
            {
                currentPackets.Add(parsed);
            }
        }

        this.LogGopPacketsSummary(
            item.Id,
            filePath,
            packets.Count,
            videoPacketCount,
            primaryVideoStreamIndex.Value
        );

        if (videoPacketCount == 0)
        {
            this.LogGopNoVideoPackets(item.Id, filePath);
            return;
        }

        if (currentPackets.Count > 0)
        {
            FinalizeGroup(currentPackets, null, index);
        }

        if (index.Groups.Count == 0)
        {
            return;
        }

        await this
            .gopIndexService.WriteAsync(metadata.Uuid, partIndex, index, cancellationToken)
            .ConfigureAwait(false);
        this.LogGopWritten(item.Id, filePath, index.Groups.Count, gopPath);
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP analyzer skipped: no parts for media item {MediaItemId}"
    )]
    private partial void LogNoParts(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP analyzer skipping blank/whitespace file path for media item {MediaItemId}"
    )]
    private partial void LogBlankPath(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP not supported: no parts for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedNoParts(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP not supported: blank part path for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedBlankPath(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GOP support eval media item {MediaItemId}: ext={Extension} isVideo={IsVideo}"
    )]
    private partial void LogSupportEvaluation(int mediaItemId, string? extension, bool isVideo);

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
        Message = "Skipping GOP XML: already exists for media item {MediaItemId} part {PartIndex}"
    )]
    private partial void LogGopAlreadyExists(int mediaItemId, int partIndex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping GOP XML: invalid UUID '{Uuid}' for media item {MediaItemId}"
    )]
    private partial void LogGopInvalidUuid(int mediaItemId, string uuid);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed writing GOP XML for media item {MediaItemId} file '{FilePath}'"
    )]
    private partial void LogGopWriteFailed(int mediaItemId, string filePath, Exception ex);

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
    #endregion

    private sealed class ParsedPacket
    {
        public required FFProbePacketAnalysis OriginalPacket { get; init; }

        public required TimeSpan Pts { get; init; }

        public required TimeSpan Duration { get; init; }

        public required bool IsKeyFrame { get; init; }
    }
}
