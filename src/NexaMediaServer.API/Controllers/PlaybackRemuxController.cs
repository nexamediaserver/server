// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Provides direct-stream (remux) playback endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/playback")]
public sealed class PlaybackRemuxController : ControllerBase
{
    private static readonly Action<ILogger, int, string, Exception?> LogRemuxStreamStarting =
        LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(1, "RemuxStreamStart"),
            "Starting remux stream for part {PartId} as {Ext}"
        );

    private static readonly Action<ILogger, int, long, Exception?> LogRemuxSeekStarting =
        LoggerMessage.Define<int, long>(
            LogLevel.Debug,
            new EventId(2, "RemuxSeekStart"),
            "Starting remux stream with seek for part {PartId} at position {SeekMs}ms"
        );

    private readonly IFfmpegCommandBuilder commandBuilder;
    private readonly IMediaPartRepository mediaPartRepository;
    private readonly TranscodeOptions transcodeOptions;
    private readonly ILogger<PlaybackRemuxController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackRemuxController"/> class.
    /// </summary>
    /// <param name="commandBuilder">FFmpeg command executor.</param>
    /// <param name="mediaPartRepository">Media part repository.</param>
    /// <param name="transcodeOptions">Transcode configuration.</param>
    /// <param name="logger">Typed logger.</param>
    public PlaybackRemuxController(
        IFfmpegCommandBuilder commandBuilder,
        IMediaPartRepository mediaPartRepository,
        IOptions<TranscodeOptions> transcodeOptions,
        ILogger<PlaybackRemuxController> logger
    )
    {
        this.commandBuilder = commandBuilder;
        this.mediaPartRepository = mediaPartRepository;
        this.transcodeOptions = transcodeOptions.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Streams a remuxed version of a media part without persisting intermediate files.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="ext">Requested container extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An empty result after piping the response body.</returns>
    [HttpGet("part/{partId}/remux.{ext}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DirectStreamAsync(
        [FromRoute] int partId,
        [FromRoute] string ext,
        CancellationToken cancellationToken
    )
    {
        var mediaPart = await this.mediaPartRepository.GetByIdAsync(partId);
        if (mediaPart == null)
        {
            return this.NotFound();
        }

        if (!System.IO.File.Exists(mediaPart.File))
        {
            return this.NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType($"dummy.{ext}", out var contentType))
        {
            contentType = "video/mp4";
        }

        LogRemuxStreamStarting(this.logger, partId, ext, null);

        this.Response.StatusCode = StatusCodes.Status200OK;
        this.Response.ContentType = contentType;
        this.Response.Headers["Cache-Control"] = "no-store";

        await this.commandBuilder.RemuxToStreamAsync(
            mediaPart.File,
            string.IsNullOrWhiteSpace(ext) ? "mp4" : ext,
            this.Response.Body,
            this.transcodeOptions.HardwareAcceleration,
            cancellationToken
        );

        return new EmptyResult();
    }

    /// <summary>
    /// Streams a remuxed version of a media part starting from a specific position.
    /// The seek position should be a keyframe timestamp obtained from the playbackSeek mutation
    /// for optimal performance.
    /// </summary>
    /// <param name="partId">Target media part identifier.</param>
    /// <param name="ext">Requested container extension.</param>
    /// <param name="seekMs">The position in milliseconds to seek to before starting the stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An empty result after piping the response body.</returns>
    [HttpGet("part/{partId}/remux-seek.{ext}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DirectStreamWithSeekAsync(
        [FromRoute] int partId,
        [FromRoute] string ext,
        [FromQuery] long seekMs,
        CancellationToken cancellationToken
    )
    {
        var mediaPart = await this.mediaPartRepository.GetByIdAsync(partId);
        if (mediaPart == null)
        {
            return this.NotFound();
        }

        if (!System.IO.File.Exists(mediaPart.File))
        {
            return this.NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType($"dummy.{ext}", out var contentType))
        {
            contentType = "video/mp4";
        }

        LogRemuxSeekStarting(this.logger, partId, seekMs, null);

        this.Response.StatusCode = StatusCodes.Status200OK;
        this.Response.ContentType = contentType;
        this.Response.Headers["Cache-Control"] = "no-store";

        await this.commandBuilder.RemuxToStreamWithSeekAsync(
            mediaPart.File,
            string.IsNullOrWhiteSpace(ext) ? "mp4" : ext,
            this.Response.Body,
            seekMs,
            this.transcodeOptions.HardwareAcceleration,
            cancellationToken
        );

        return new EmptyResult();
    }
}
