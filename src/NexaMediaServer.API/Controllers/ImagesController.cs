// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Images API for transcoding and serving image variants.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/images")]
#pragma warning disable S6960 // Controller handles related image operations (transcode + trickplay)
public class ImagesController : ControllerBase
#pragma warning restore S6960
{
    private readonly IImageService imageService;
    private readonly IBifService bifService;
    private readonly IMediaPartRepository mediaPartRepository;
    private readonly IMemoryCache memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImagesController"/> class.
    /// </summary>
    /// <param name="imageService">Image service to handle transcoding and retrieval.</param>
    /// <param name="bifService">BIF service for trickplay thumbnail data.</param>
    /// <param name="mediaPartRepository">Repository for media parts.</param>
    /// <param name="memoryCache">Memory cache for BIF file caching.</param>
    public ImagesController(
        IImageService imageService,
        IBifService bifService,
        IMediaPartRepository mediaPartRepository,
        IMemoryCache memoryCache
    )
    {
        this.imageService = imageService;
        this.bifService = bifService;
        this.mediaPartRepository = mediaPartRepository;
        this.memoryCache = memoryCache;
    }

    /// <summary>
    /// Transcode or retrieve an image variant.
    /// </summary>
    /// <param name="uri">Internal image URI (metadata://...)</param>
    /// <param name="width">Desired width in pixels (optional).</param>
    /// <param name="height">Desired height in pixels (optional).</param>
    /// <param name="format">Output format (jpg, png, webp, avif, ...).</param>
    /// <param name="quality">Quality hint for lossy formats (0-100).</param>
    /// <param name="preserveAspectRatio">Whether to preserve aspect ratio (default true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcoded image file result.</returns>
    [HttpGet("transcode")]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Transcode(
        [FromQuery] string uri,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] string? format,
        [FromQuery] int? quality,
        [FromQuery] bool? preserveAspectRatio,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var req = new ImageTranscodeRequest
            {
                SourceUri = uri,
                Width = width,
                Height = height,
                Format = format,
                Quality = quality,
                PreserveAspectRatio = preserveAspectRatio ?? true,
            };

            var outputPath = await this.imageService.GetOrTranscodeAsync(req, cancellationToken);

            // Generate ETag based on file path and last write time
            var fileInfo = new FileInfo(outputPath);
            var etagValue = GenerateETag(outputPath, fileInfo.LastWriteTimeUtc);
            var etag = new EntityTagHeaderValue($"\"{etagValue}\"");

            // Check If-None-Match header for conditional request
            if (this.Request.Headers.IfNoneMatch.Count > 0)
            {
                var requestEtag = this.Request.Headers.IfNoneMatch.FirstOrDefault();
                if (
                    requestEtag != null
                    && EntityTagHeaderValue.TryParse(requestEtag, out var parsedEtag)
                    && etag.Equals(parsedEtag)
                )
                {
                    return this.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(outputPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var fileName = System.IO.Path.GetFileName(outputPath);

            // Set ETag header
            this.Response.Headers.ETag = etag.ToString();

            return this.PhysicalFile(
                outputPath,
                contentType,
                fileName,
                enableRangeProcessing: true
            );
        }
        catch (FileNotFoundException ex)
        {
            return this.Problem(
                title: "Source image not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }
        catch (Exception ex)
        {
            return this.Problem(
                title: "Failed to transcode image",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get WebVTT thumbnail track for a media part's trickplay data.
    /// </summary>
    /// <param name="mediaPartId">The media part identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WebVTT file content or empty VTT if no BIF file exists.</returns>
    [HttpGet("trickplay/{mediaPartId}.vtt")]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrickplayVtt(
        int mediaPartId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var mediaPart = await this
                .mediaPartRepository.GetQueryable()
                .Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
                .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken);

            if (mediaPart?.MediaItem?.MetadataItem == null)
            {
                return this.NotFound($"Media part {mediaPartId} not found");
            }

            var metadata = mediaPart.MediaItem.MetadataItem;

            // Determine part index within the media item
            int partIndex = 0;
            if (mediaPart.MediaItem.Parts is { Count: > 0 })
            {
                var list = mediaPart.MediaItem.Parts.ToList();
                partIndex = Math.Max(0, list.IndexOf(mediaPart));
            }

            // Use metadata-only loading since VTT doesn't need image data
            // This is much faster and uses less memory
            var cacheKey = $"bif_metadata_{mediaPartId}";
            var bifFile = await this.memoryCache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    var bif = await this.bifService.TryReadMetadataAsync(
                        metadata,
                        partIndex,
                        cancellationToken
                    );

                    if (bif != null)
                    {
                        // Metadata is much smaller than full BIF
                        entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                        entry.SetAbsoluteExpiration(TimeSpan.FromHours(24));
                        entry.SetSize(bif.Entries.Count * 16); // Just the entries
                        entry.SetPriority(CacheItemPriority.High); // Metadata is small, keep it
                    }
                    else
                    {
                        // Cache null result briefly to avoid repeated lookups
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                        entry.SetSize(1);
                    }

                    return bif;
                }
            );

            // Generate ETag based on mediaPartId and entry count
            var entryCount = bifFile?.Entries.Count ?? 0;
            var etagValue = GenerateETag($"vtt_{mediaPartId}_{entryCount}", DateTime.UtcNow.Date);
            var etag = new EntityTagHeaderValue($"\"{etagValue}\"");

            // Check If-None-Match header for conditional request
            if (this.Request.Headers.IfNoneMatch.Count > 0)
            {
                var requestEtag = this.Request.Headers.IfNoneMatch.FirstOrDefault();
                if (
                    requestEtag != null
                    && EntityTagHeaderValue.TryParse(requestEtag, out var parsedEtag)
                    && etag.Equals(parsedEtag)
                )
                {
                    return this.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Generate WebVTT content
            var vttContent = GenerateWebVttContent(mediaPartId, bifFile);

            // Set ETag header
            this.Response.Headers.ETag = etag.ToString();

            return this.Content(vttContent, "text/vtt");
        }
        catch (Exception ex)
        {
            return this.Problem(
                title: "Failed to generate trickplay VTT",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get a specific thumbnail image from a media part's BIF file.
    /// </summary>
    /// <param name="mediaPartId">The media part identifier.</param>
    /// <param name="index">The thumbnail index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG image data.</returns>
    [HttpGet("trickplay/{mediaPartId}/thumb/{index}.jpg")]
    [ResponseCache(Duration = 2592000, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrickplayThumbnail(
        int mediaPartId,
        int index,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var mediaPart = await this
                .mediaPartRepository.GetQueryable()
                .Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
                .FirstOrDefaultAsync(p => p.Id == mediaPartId, cancellationToken);

            if (mediaPart?.MediaItem?.MetadataItem == null)
            {
                return this.NotFound($"Media part {mediaPartId} not found");
            }

            var metadata = mediaPart.MediaItem.MetadataItem;

            // Determine part index within the media item
            int partIndex = 0;
            if (mediaPart.MediaItem.Parts is { Count: > 0 })
            {
                var list = mediaPart.MediaItem.Parts.ToList();
                partIndex = Math.Max(0, list.IndexOf(mediaPart));
            }

            // Generate ETag based on mediaPartId and index
            var etagValue = GenerateETag($"thumb_{mediaPartId}_{index}", DateTime.UtcNow.Date);
            var etag = new EntityTagHeaderValue($"\"{etagValue}\"");

            // Check If-None-Match header for conditional request
            if (this.Request.Headers.IfNoneMatch.Count > 0)
            {
                var requestEtag = this.Request.Headers.IfNoneMatch.FirstOrDefault();
                if (
                    requestEtag != null
                    && EntityTagHeaderValue.TryParse(requestEtag, out var parsedEtag)
                    && etag.Equals(parsedEtag)
                )
                {
                    return this.StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Use lazy loading to read only the specific thumbnail
            // Cache individual thumbnails instead of entire BIF file
            var thumbnailCacheKey = $"bif_thumb_{mediaPartId}_{index}";
            var imageBytes = await this.memoryCache.GetOrCreateAsync(
                thumbnailCacheKey,
                async entry =>
                {
                    var thumbnail = await this.bifService.TryReadThumbnailAsync(
                        metadata,
                        partIndex,
                        index,
                        cancellationToken
                    );

                    if (thumbnail != null)
                    {
                        // Cache individual thumbnail
                        entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                        entry.SetAbsoluteExpiration(TimeSpan.FromHours(24));
                        entry.SetSize(thumbnail.Length);
                        entry.SetPriority(CacheItemPriority.Normal);
                    }
                    else
                    {
                        // Cache null result briefly
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                        entry.SetSize(1);
                    }

                    return thumbnail;
                }
            );

            if (imageBytes == null)
            {
                return this.NotFound($"Thumbnail {index} not found for media part {mediaPartId}");
            }

            // Set ETag header
            this.Response.Headers.ETag = etag.ToString();

            return this.File(imageBytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            return this.Problem(
                title: "Failed to retrieve trickplay thumbnail",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Invalidates the cached BIF file for a specific media part.
    /// This should be called when a BIF file is regenerated or deleted.
    /// </summary>
    /// <param name="mediaPartId">The media part identifier.</param>
    internal void InvalidateBifCache(int mediaPartId)
    {
        // Remove metadata cache
        var metadataCacheKey = $"bif_metadata_{mediaPartId}";
        this.memoryCache.Remove(metadataCacheKey);

        // Note: Individual thumbnail caches (bif_thumb_{mediaPartId}_{index})
        // will expire naturally based on their TTL. If needed, we could track
        // and remove them, but it would require maintaining a list of cached indices.
    }

    /// <summary>
    /// Generates WebVTT content for thumbnail track.
    /// </summary>
    private static string GenerateWebVttContent(int mediaPartId, BifFile? bifFile)
    {
        var subtitle = new Subtitle();
        var format = new WebVTT();

        if (bifFile == null || bifFile.Entries.Count == 0)
        {
            // Return valid but empty VTT if no BIF file exists
            return format.ToText(subtitle, string.Empty);
        }

        // Generate a paragraph (cue) for each thumbnail
        for (int i = 0; i < bifFile.Entries.Count; i++)
        {
            var entry = bifFile.Entries[i];
            var startTime = TimeSpan.FromMilliseconds(entry.TimestampMs);

            // Determine end time (use next entry's timestamp or add a default duration)
            TimeSpan endTime;
            if (i < bifFile.Entries.Count - 1)
            {
                endTime = TimeSpan.FromMilliseconds(bifFile.Entries[i + 1].TimestampMs);
            }
            else
            {
                // For the last entry, add a reasonable duration (e.g., 5 seconds)
                endTime = startTime.Add(TimeSpan.FromSeconds(5));
            }

            // Create a paragraph (cue) with the thumbnail URL
            var paragraph = new Paragraph
            {
                StartTime = new TimeCode(startTime),
                EndTime = new TimeCode(endTime),
                Text = $"/api/v1/images/trickplay/{mediaPartId}/thumb/{i}.jpg",
            };

            subtitle.Paragraphs.Add(paragraph);
        }

        return format.ToText(subtitle, string.Empty);
    }

    /// <summary>
    /// Generates an ETag value based on a resource identifier and timestamp.
    /// </summary>
    /// <param name="identifier">Resource identifier (e.g., file path).</param>
    /// <param name="timestamp">Timestamp representing the resource version.</param>
    /// <returns>ETag value as a hexadecimal string.</returns>
    private static string GenerateETag(string identifier, DateTime timestamp)
    {
        var input = $"{identifier}:{timestamp:O}";
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);

        // Use first 16 bytes for a shorter ETag
        return Convert.ToHexString(hashBytes[..16]).ToLowerInvariant();
    }
}
