// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.API.Controllers;

/// <summary>
/// Media API for serving media files directly.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaPartRepository mediaPartRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaController"/> class.
    /// </summary>
    /// <param name="mediaPartRepository">Media part repository to retrieve file paths.</param>
    public MediaController(IMediaPartRepository mediaPartRepository)
    {
        this.mediaPartRepository = mediaPartRepository;
    }

    /// <summary>
    /// Directly stream a media file to the client.
    /// </summary>
    /// <param name="partId">The ID of the media part.</param>
    /// <param name="ext">The file extension (from route).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The media file stream.</returns>
    [HttpGet("part/{partId}/file.{ext}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DirectPlay(
        [FromRoute] int partId,
        [FromRoute] string ext,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Retrieve the media part from the repository
            var mediaPart = await this.mediaPartRepository.GetByIdAsync(partId);

            if (mediaPart == null)
            {
                return this.Problem(
                    title: "Media part not found",
                    detail: $"No media part found with ID {partId}",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            // Get the file path
            var filePath = mediaPart.File;

            // Verify the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return this.Problem(
                    title: "Media file not found",
                    detail: $"The file at path '{filePath}' does not exist",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            // Verify the file extension matches the requested extension
            var actualExt = System.IO.Path.GetExtension(filePath).TrimStart('.');
            if (!string.Equals(actualExt, ext, StringComparison.OrdinalIgnoreCase))
            {
                return this.Problem(
                    title: "File extension mismatch",
                    detail: $"Requested extension '{ext}' does not match actual file extension '{actualExt}'",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            // Determine content type
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var fileName = System.IO.Path.GetFileName(filePath);

            // Stream the file directly
            return this.PhysicalFile(filePath, contentType, fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            return this.Problem(
                title: "Failed to serve media file",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
