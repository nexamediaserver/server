// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Provides DASH playback endpoints (manifest and segments).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/playback")]
public sealed class PlaybackDashController : ControllerBase
{
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

    private readonly IDashTranscodeService dashTranscodeService;
    private readonly ILogger<PlaybackDashController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackDashController"/> class.
    /// </summary>
    /// <param name="dashTranscodeService">Service that materializes DASH artifacts.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaybackDashController(
        IDashTranscodeService dashTranscodeService,
        ILogger<PlaybackDashController> logger
    )
    {
        this.dashTranscodeService = dashTranscodeService;
        this.logger = logger;
    }

    /// <summary>
    /// Returns or generates a DASH manifest for the requested media part.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated manifest file.</returns>
    [HttpGet("part/{partId}/dash/manifest.mpd")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashManifestAsync(
        [FromRoute] int partId,
        CancellationToken cancellationToken
    )
    {
        var result = await this.dashTranscodeService.EnsureDashAsync(partId, cancellationToken);
        LogDashManifestServed(this.logger, partId, result.ManifestPath, null);
        return this.PhysicalFile(result.ManifestPath, "application/dash+xml");
    }

    /// <summary>
    /// Returns or generates a DASH manifest starting from a specific seek position.
    /// The transcode will begin from the nearest keyframe to the requested position.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="seekMs">The position in milliseconds to start transcoding from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated manifest file and the actual start time.</returns>
    [HttpGet("part/{partId}/dash-seek/manifest.mpd")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashSeekManifestAsync(
        [FromRoute] int partId,
        [FromQuery] long seekMs,
        CancellationToken cancellationToken
    )
    {
        var result = await this.dashTranscodeService.EnsureDashWithSeekAsync(
            partId,
            seekMs,
            cancellationToken
        );
        LogDashSeekManifestServed(this.logger, partId, seekMs, result.ManifestPath, null);

        // Include the actual start time in a custom header so the client knows
        // where the transcoded content actually starts (may differ from requested seekMs)
        this.Response.Headers["X-Dash-Start-Time-Ms"] = result.StartTimeMs.ToString(
            System.Globalization.CultureInfo.InvariantCulture
        );

        return this.PhysicalFile(result.ManifestPath, "application/dash+xml");
    }

    /// <summary>
    /// Serves a DASH segment from a seek-based transcode.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="seekMs">The seek position that was used to generate the segments.</param>
    /// <param name="fileName">Segment file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested segment file.</returns>
    [HttpGet("part/{partId}/dash-seek/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashSeekSegmentAsync(
        [FromRoute] int partId,
        [FromQuery] long seekMs,
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

        var result = await this.dashTranscodeService.EnsureDashWithSeekAsync(
            partId,
            seekMs,
            cancellationToken
        );
        var segmentPath = System.IO.Path.Combine(result.OutputDirectory, fileName);
        if (!System.IO.File.Exists(segmentPath))
        {
            return this.NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(segmentPath, out var contentType))
        {
            contentType = "video/mp4";
        }

        LogDashSegmentServed(this.logger, fileName, partId, null);
        return this.PhysicalFile(segmentPath, contentType);
    }

    /// <summary>
    /// Serves a DASH segment or initialization fragment.
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

        var result = await this.dashTranscodeService.EnsureDashAsync(partId, cancellationToken);
        var segmentPath = System.IO.Path.Combine(result.OutputDirectory, fileName);
        if (!System.IO.File.Exists(segmentPath))
        {
            return this.NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(segmentPath, out var contentType))
        {
            contentType = "video/mp4";
        }

        LogDashSegmentServed(this.logger, fileName, partId, null);
        return this.PhysicalFile(segmentPath, contentType);
    }
}
