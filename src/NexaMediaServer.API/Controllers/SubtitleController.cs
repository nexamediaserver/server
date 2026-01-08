// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Controller for subtitle delivery and conversion.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed partial class SubtitleController : ControllerBase
{
    private readonly ISubtitleConversionService subtitleConversionService;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<SubtitleController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleController"/> class.
    /// </summary>
    /// <param name="subtitleConversionService">The subtitle conversion service.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="logger">The logger.</param>
    public SubtitleController(
        ISubtitleConversionService subtitleConversionService,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ILogger<SubtitleController> logger)
    {
        this.subtitleConversionService = subtitleConversionService;
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    /// <summary>
    /// Gets a subtitle in the requested format for a media part.
    /// </summary>
    /// <param name="partId">The media part ID.</param>
    /// <param name="streamIndex">The subtitle stream index within the container.</param>
    /// <param name="format">The output format (vtt, srt, ass).</param>
    /// <param name="startPositionTicks">Optional start position in ticks.</param>
    /// <param name="endPositionTicks">Optional end position in ticks.</param>
    /// <param name="addVttTimeMap">Whether to add X-TIMESTAMP-MAP header for HLS sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The converted subtitle file.</returns>
    [HttpGet("part/{partId:int}/stream/{streamIndex:int}/stream.{format}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetSubtitle(
        int partId,
        int streamIndex,
        string format,
        [FromQuery] long? startPositionTicks = null,
        [FromQuery] long? endPositionTicks = null,
        [FromQuery] bool addVttTimeMap = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Get the media part file path
        var part = await context.MediaParts
            .AsNoTracking()
            .Where(p => p.Id == partId)
            .Select(p => new { p.File, p.Duration })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (part == null)
        {
            return this.NotFound(new { message = "Media part not found." });
        }

        try
        {
            // Extract and convert subtitle using FFmpeg
            // Note: We don't have detailed stream metadata in the current model,
            // so we rely on the stream index being valid and FFmpeg to handle extraction.
            var resultStream = await this.subtitleConversionService.ExtractFromMediaAsync(
                part.File,
                streamIndex,
                format,
                cancellationToken).ConfigureAwait(false);

            // Add X-TIMESTAMP-MAP header for HLS synchronization if requested
            if (addVttTimeMap && format.Equals("vtt", StringComparison.OrdinalIgnoreCase))
            {
                resultStream = await AddVttTimeMapAsync(resultStream, cancellationToken).ConfigureAwait(false);
            }

            var contentType = this.subtitleConversionService.GetMimeType(format);
            return this.File(resultStream, contentType);
        }
        catch (FileNotFoundException ex)
        {
            this.LogSubtitleNotFound(partId, streamIndex, ex.Message);
            return this.NotFound(new { message = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            this.LogUnsupportedFormat(format, ex.Message);
            return this.BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets an HLS subtitle playlist for segmented subtitle delivery.
    /// </summary>
    /// <param name="partId">The media part ID.</param>
    /// <param name="streamIndex">The subtitle stream index.</param>
    /// <param name="segmentLength">The segment length in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An HLS subtitle playlist.</returns>
    [HttpGet("part/{partId:int}/stream/{streamIndex:int}/playlist.m3u8")]
    [ProducesResponseType(typeof(string), 200, "application/x-mpegurl")]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSubtitlePlaylist(
        int partId,
        int streamIndex,
        [FromQuery] int segmentLength = 6,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var part = await context.MediaParts
            .AsNoTracking()
            .Where(p => p.Id == partId)
            .Select(p => new { p.Duration })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (part == null)
        {
            return this.NotFound(new { message = "Media part not found." });
        }

        var durationSeconds = part.Duration ?? 0;
        if (durationSeconds <= 0)
        {
            return this.BadRequest(new { message = "Media part has no valid duration." });
        }

        var durationTicks = TimeSpan.FromSeconds(durationSeconds).Ticks;
        var segmentLengthTicks = TimeSpan.FromSeconds(segmentLength).Ticks;

        var playlist = new StringBuilder();
        playlist.AppendLine("#EXTM3U");
        playlist.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-TARGETDURATION:{segmentLength}");
        playlist.AppendLine("#EXT-X-VERSION:3");
        playlist.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        playlist.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");

        long positionTicks = 0;
        while (positionTicks < durationTicks)
        {
            var remaining = durationTicks - positionTicks;
            var lengthTicks = Math.Min(remaining, segmentLengthTicks);
            var lengthSeconds = TimeSpan.FromTicks(lengthTicks).TotalSeconds;

            var endPositionTicks = positionTicks + lengthTicks;

            playlist.AppendLine(CultureInfo.InvariantCulture, $"#EXTINF:{lengthSeconds:F3},");
            playlist.AppendLine(CultureInfo.InvariantCulture, $"stream.vtt?startPositionTicks={positionTicks}&endPositionTicks={endPositionTicks}&addVttTimeMap=true");

            positionTicks += segmentLengthTicks;
        }

        playlist.AppendLine("#EXT-X-ENDLIST");

        return this.Content(playlist.ToString(), "application/x-mpegurl", Encoding.UTF8);
    }

    private static async Task<Stream> AddVttTimeMapAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        // Insert X-TIMESTAMP-MAP after WEBVTT header
        content = content.Replace(
            "WEBVTT",
            "WEBVTT\nX-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000",
            StringComparison.Ordinal);

        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Subtitle not found for part {PartId} stream {StreamIndex}: {Message}")]
    private partial void LogSubtitleNotFound(int partId, int streamIndex, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported subtitle format {Format}: {Message}")]
    private partial void LogUnsupportedFormat(string format, string message);
}
