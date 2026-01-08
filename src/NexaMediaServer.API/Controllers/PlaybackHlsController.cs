// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Infrastructure.Services;

using Path = System.IO.Path;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Provides HLS playback endpoints (master playlist, variant playlists, and segments).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/playback")]
public sealed partial class PlaybackHlsController : ControllerBase
{
    private readonly IHlsTranscodeService hlsTranscodeService;
    private readonly ILogger<PlaybackHlsController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackHlsController"/> class.
    /// </summary>
    /// <param name="hlsTranscodeService">Service that materializes HLS artifacts.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaybackHlsController(
        IHlsTranscodeService hlsTranscodeService,
        ILogger<PlaybackHlsController> logger
    )
    {
        this.hlsTranscodeService = hlsTranscodeService;
        this.logger = logger;
    }

    /// <summary>
    /// Returns or generates an HLS master playlist for the requested media part.
    /// Optionally starts transcoding from a specific seek position.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="seekMs">Optional position in milliseconds to start transcoding from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated master playlist file.</returns>
    [HttpGet("part/{partId}/hls/master.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHlsMasterPlaylistAsync(
        [FromRoute] int partId,
        [FromQuery] long? seekMs,
        CancellationToken cancellationToken
    )
    {
        if (seekMs.HasValue && seekMs.Value > 0)
        {
            var seekResult = await this.hlsTranscodeService.EnsureHlsWithSeekAsync(
                partId,
                seekMs.Value,
                abrLadder: null,
                cancellationToken
            );

            this.Response.Headers["X-Hls-Start-Time-Ms"] = seekResult.StartTimeMs.ToString(
                CultureInfo.InvariantCulture
            );

            this.LogHlsSeekMasterServed(partId, seekMs.Value, seekResult.MasterPlaylistPath);
            return this.PhysicalFile(seekResult.MasterPlaylistPath, "application/vnd.apple.mpegurl");
        }

        var result = await this.hlsTranscodeService.EnsureHlsAsync(
            partId,
            abrLadder: null,
            cancellationToken
        );

        this.LogHlsMasterServed(partId, result.MasterPlaylistPath);
        return this.PhysicalFile(result.MasterPlaylistPath, "application/vnd.apple.mpegurl");
    }

    /// <summary>
    /// Returns or generates an HLS variant playlist for a specific quality level.
    /// Optionally serves from a seek-based transcode.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="variantId">Variant identifier (e.g., "720p", "auto").</param>
    /// <param name="seekMs">Optional seek position used to generate the segments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The variant playlist file.</returns>
    [HttpGet("part/{partId}/hls/{variantId}/playlist.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHlsVariantPlaylistAsync(
        [FromRoute] int partId,
        [FromRoute] string variantId,
        [FromQuery] long? seekMs,
        CancellationToken cancellationToken
    )
    {
        if (ContainsPathTraversal(variantId))
        {
            return this.BadRequest("Invalid variant identifier.");
        }

        Dictionary<string, string> variantPaths;
        if (seekMs.HasValue && seekMs.Value > 0)
        {
            var seekResult = await this.hlsTranscodeService.EnsureHlsWithSeekAsync(
                partId,
                seekMs.Value,
                abrLadder: null,
                cancellationToken
            );
            variantPaths = seekResult.VariantPaths;
        }
        else
        {
            var result = await this.hlsTranscodeService.EnsureHlsAsync(
                partId,
                abrLadder: null,
                cancellationToken
            );
            variantPaths = result.VariantPaths;
        }

        if (!variantPaths.TryGetValue(variantId, out var playlistPath))
        {
            return this.NotFound();
        }

        if (!System.IO.File.Exists(playlistPath))
        {
            return this.NotFound();
        }

        this.LogHlsVariantServed(partId, variantId, playlistPath);
        return this.PhysicalFile(playlistPath, "application/vnd.apple.mpegurl");
    }

    /// <summary>
    /// Serves an HLS segment or initialization fragment.
    /// Optionally serves from a seek-based transcode.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="variantId">Variant identifier.</param>
    /// <param name="fileName">Segment file name.</param>
    /// <param name="seekMs">Optional seek position used to generate the segments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested segment file.</returns>
    [HttpGet("part/{partId}/hls/{variantId}/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHlsSegmentAsync(
        [FromRoute] int partId,
        [FromRoute] string variantId,
        [FromRoute] string fileName,
        [FromQuery] long? seekMs,
        CancellationToken cancellationToken
    )
    {
        if (ContainsPathTraversal(variantId) || ContainsPathTraversal(fileName))
        {
            return this.BadRequest("Invalid segment path.");
        }

        string outputDirectory;
        if (seekMs.HasValue && seekMs.Value > 0)
        {
            var seekResult = await this.hlsTranscodeService.EnsureHlsWithSeekAsync(
                partId,
                seekMs.Value,
                abrLadder: null,
                cancellationToken
            );
            outputDirectory = seekResult.OutputDirectory;
        }
        else
        {
            var result = await this.hlsTranscodeService.EnsureHlsAsync(
                partId,
                abrLadder: null,
                cancellationToken
            );
            outputDirectory = result.OutputDirectory;
        }

        var segmentPath = Path.Combine(outputDirectory, variantId, fileName);

        // Wait for segment to be created if transcode is in progress
        if (!System.IO.File.Exists(segmentPath))
        {
            var segmentReady = await this.hlsTranscodeService.WaitForSegmentAsync(
                segmentPath,
                cancellationToken
            );
            if (!segmentReady)
            {
                return this.NotFound();
            }
        }

        var contentType = GetSegmentContentType(segmentPath);

        this.LogHlsSegmentServed(fileName, partId, variantId);
        return this.PhysicalFile(segmentPath, contentType);
    }

    private static bool ContainsPathTraversal(string path)
    {
        return path.Contains("..", StringComparison.Ordinal)
            || path.Contains('/')
            || path.Contains('\\');
    }

    private static string GetSegmentContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".m3u8" => "application/vnd.apple.mpegurl",
            ".ts" => "video/mp2t",
            ".m4s" => "video/mp4",
            ".mp4" => "video/mp4",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            _ => "application/octet-stream",
        };
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serving HLS master playlist for part {PartId} from {Path}")]
    private partial void LogHlsMasterServed(int partId, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serving seek-based HLS master playlist for part {PartId} at {SeekMs}ms from {Path}")]
    private partial void LogHlsSeekMasterServed(int partId, long seekMs, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serving HLS variant playlist for part {PartId} variant {VariantId} from {Path}")]
    private partial void LogHlsVariantServed(int partId, string variantId, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serving HLS segment {FileName} for part {PartId} variant {VariantId}")]
    private partial void LogHlsSegmentServed(string fileName, int partId, string variantId);
}
