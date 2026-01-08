// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Provides DASH playback endpoints (manifest and segments).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/playback")]
public sealed partial class PlaybackDashController : ControllerBase
{
    /// <summary>
    /// The gap (in segments) before we restart transcoding when seeking forward.
    /// Based on Jellyfin's approach: 24 seconds worth of segments.
    /// </summary>
    private const int ForwardSeekGapThresholdSeconds = 24;

    /// <summary>
    /// Regex to extract segment index from DASH segment filenames.
    /// Matches patterns like "chunk-stream0-00001.m4s" -> captures "00001".
    /// </summary>
    private static readonly Regex SegmentIndexRegex = CreateSegmentIndexRegex();

    private static readonly Action<ILogger, int, string, Exception?> LogDashManifestServed =
        LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(1, "DashManifestServe"),
            "Serving DASH manifest for part {PartId} from {Path}"
        );

    private static readonly Action<ILogger, string, int, Exception?> LogDashSegmentServed =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(2, "DashSegmentServe"),
            "Serving DASH segment {File} for part {PartId}"
        );

    private static readonly Action<
        ILogger,
        int,
        long,
        string,
        Exception?
    > LogDashSeekManifestServed = LoggerMessage.Define<int, long, string>(
        LogLevel.Debug,
        new EventId(3, "DashSeekManifestServe"),
        "Serving seek-based DASH manifest for part {PartId} at {SeekMs}ms from {Path}"
    );

    private static readonly Action<ILogger, int, int?, int, Exception?> LogSegmentBehindTranscode =
        LoggerMessage.Define<int, int?, int>(
            LogLevel.Debug,
            new EventId(4, "SegmentBehindTranscode"),
            "Segment {RequestedIndex} is behind current transcode position {CurrentIndex}, restarting from segment {RestartIndex}"
        );

    private static readonly Action<ILogger, int, int?, int, Exception?> LogSegmentAheadOfTranscode =
        LoggerMessage.Define<int, int?, int>(
            LogLevel.Debug,
            new EventId(5, "SegmentAheadOfTranscode"),
            "Segment {RequestedIndex} is too far ahead of current transcode position {CurrentIndex}, restarting from segment {RestartIndex}"
        );

    private readonly IDashTranscodeService dashTranscodeService;
    private readonly ITranscodeJobCache jobCache;
    private readonly ILogger<PlaybackDashController> logger;
    private readonly IOptionsMonitor<TranscodeOptions> transcodeOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackDashController"/> class.
    /// </summary>
    /// <param name="dashTranscodeService">Service that materializes DASH artifacts.</param>
    /// <param name="jobCache">In-memory cache for active transcode jobs.</param>
    /// <param name="transcodeOptions">Transcode configuration options.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaybackDashController(
        IDashTranscodeService dashTranscodeService,
        ITranscodeJobCache jobCache,
        IOptionsMonitor<TranscodeOptions> transcodeOptions,
        ILogger<PlaybackDashController> logger
    )
    {
        this.dashTranscodeService = dashTranscodeService;
        this.jobCache = jobCache;
        this.transcodeOptions = transcodeOptions;
        this.logger = logger;
    }

    /// <summary>
    /// Returns or generates a DASH manifest for the requested media part.
    /// Optionally starts transcoding from a specific seek position.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="seekMs">Optional position in milliseconds to start transcoding from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated manifest file.</returns>
    [HttpGet("part/{partId}/dash/manifest.mpd")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashManifestAsync(
        [FromRoute] int partId,
        [FromQuery] long? seekMs,
        CancellationToken cancellationToken
    )
    {
        if (seekMs.HasValue && seekMs.Value > 0)
        {
            var seekResult = await this.dashTranscodeService.EnsureDashWithSeekAsync(
                partId,
                seekMs.Value,
                startSegmentNumber: null,
                cancellationToken
            );
            LogDashSeekManifestServed(this.logger, partId, seekMs.Value, seekResult.ManifestPath, null);

            this.Response.Headers["X-Dash-Start-Time-Ms"] = seekResult.StartTimeMs.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            );

            return this.PhysicalFile(seekResult.ManifestPath, "application/dash+xml");
        }

        var result = await this.dashTranscodeService.EnsureDashAsync(partId, cancellationToken);
        LogDashManifestServed(this.logger, partId, result.ManifestPath, null);
        return this.PhysicalFile(result.ManifestPath, "application/dash+xml");
    }

    /// <summary>
    /// Serves a DASH segment or initialization fragment.
    /// Uses Jellyfin-style smart segment serving: if the requested segment is too far
    /// ahead or behind the current transcode position, restarts from the new position.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="fileName">Segment file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested segment file.</returns>
    [HttpGet("part/{partId}/dash/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashSegmentAsync(
        [FromRoute] int partId,
        [FromRoute] string fileName,
        CancellationToken cancellationToken
    )
    {
        if (
            fileName.Contains("..", StringComparison.Ordinal)
            || fileName.Contains('/', StringComparison.Ordinal)
        )
        {
            return this.BadRequest("Invalid segment name.");
        }

        // Ensure the DASH transcode exists (creates it if needed)
        var result = await this.dashTranscodeService.EnsureDashAsync(partId, cancellationToken);
        var outputDirectory = result.OutputDirectory;
        var segmentPath = System.IO.Path.Combine(outputDirectory, fileName);

        // For init segments, just serve them directly
        if (fileName.StartsWith("init-", StringComparison.OrdinalIgnoreCase))
        {
            return await this.ServeSegmentAsync(segmentPath, fileName, partId, cancellationToken);
        }

        // Parse segment index from filename
        int? requestedSegmentIndex = ParseSegmentIndex(fileName);
        if (requestedSegmentIndex == null)
        {
            // Can't parse segment index, just serve as-is
            return await this.ServeSegmentAsync(segmentPath, fileName, partId, cancellationToken);
        }

        // Check if segment already exists
        if (System.IO.File.Exists(segmentPath))
        {
            // Touch the job to keep it alive (LRU update)
            var entry = this.jobCache.GetByPath(outputDirectory);
            if (entry != null)
            {
                this.jobCache.Touch(entry.JobId);
            }

            LogDashSegmentServed(this.logger, fileName, partId, null);
            return this.ServePhysicalFile(segmentPath);
        }

        // Segment doesn't exist yet - check if we need to restart transcoding
        var currentTranscodingIndex = this.jobCache.GetCurrentTranscodingIndex(outputDirectory);
        var options = this.transcodeOptions.CurrentValue;
        var segmentLength = options.DashSegmentDurationSeconds;
        var segmentGapThreshold = ForwardSeekGapThresholdSeconds / segmentLength;

        bool shouldRestart = false;
        long restartAtMs = 0;

        if (currentTranscodingIndex == null)
        {
            // No active transcode, need to start one
            shouldRestart = true;
            restartAtMs = requestedSegmentIndex.Value * segmentLength * 1000;
        }
        else if (requestedSegmentIndex < currentTranscodingIndex)
        {
            // Requested segment is behind current position - can't go backwards, must restart
            LogSegmentBehindTranscode(
                this.logger,
                requestedSegmentIndex.Value,
                currentTranscodingIndex,
                requestedSegmentIndex.Value,
                null
            );
            shouldRestart = true;
            restartAtMs = requestedSegmentIndex.Value * segmentLength * 1000;
        }
        else if (requestedSegmentIndex - currentTranscodingIndex > segmentGapThreshold)
        {
            // Requested segment is too far ahead - faster to restart than wait
            LogSegmentAheadOfTranscode(
                this.logger,
                requestedSegmentIndex.Value,
                currentTranscodingIndex,
                requestedSegmentIndex.Value,
                null
            );
            shouldRestart = true;
            restartAtMs = requestedSegmentIndex.Value * segmentLength * 1000;
        }

        if (shouldRestart && restartAtMs > 0)
        {
            // Restart the transcode from the new position
            // Note: EnsureDashWithSeekAsync uses the same output directory but clears old segments
            // and restarts FFmpeg with -start_number matching the requested segment index
            var seekResult = await this.dashTranscodeService.EnsureDashWithSeekAsync(
                partId,
                restartAtMs,
                requestedSegmentIndex,
                cancellationToken
            );

            // Set response header to inform client of the stream offset
            this.Response.Headers["X-Dash-Start-Time-Ms"] = seekResult.StartTimeMs.ToString(
                CultureInfo.InvariantCulture
            );
        }

        // Wait for segment to be created
        return await this.ServeSegmentAsync(segmentPath, fileName, partId, cancellationToken);
    }

    [GeneratedRegex(@"-(\d+)(?:\.[^.]+)?$", RegexOptions.Compiled)]
    private static partial Regex CreateSegmentIndexRegex();

    /// <summary>
    /// Parses the segment index from a DASH segment filename.
    /// </summary>
    /// <param name="fileName">The segment filename (e.g., "chunk-stream0-00001.m4s").</param>
    /// <returns>The segment index, or null if it could not be parsed.</returns>
    private static int? ParseSegmentIndex(string fileName)
    {
        var match = SegmentIndexRegex.Match(fileName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
        {
            return index;
        }

        return null;
    }

    /// <summary>
    /// Serves a segment file, waiting for it to be created if necessary.
    /// </summary>
    private async Task<IActionResult> ServeSegmentAsync(
        string segmentPath,
        string fileName,
        int partId,
        CancellationToken cancellationToken
    )
    {
        if (!System.IO.File.Exists(segmentPath))
        {
            var segmentReady = await this.dashTranscodeService.WaitForSegmentAsync(
                segmentPath,
                cancellationToken
            );
            if (!segmentReady)
            {
                return this.NotFound();
            }
        }

        LogDashSegmentServed(this.logger, fileName, partId, null);
        return this.ServePhysicalFile(segmentPath);
    }

    /// <summary>
    /// Returns a file result with the appropriate content type.
    /// </summary>
    private PhysicalFileResult ServePhysicalFile(string segmentPath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(segmentPath, out var contentType))
        {
            contentType = "video/mp4";
        }

        return this.PhysicalFile(segmentPath, contentType);
    }
}
