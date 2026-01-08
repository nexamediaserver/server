// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

using NetVips;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

using ImageMetadata = NexaMediaServer.Core.DTOs.Metadata.Image;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Image provider using LibVips to generate thumbnails from image files.
/// </summary>
/// <remarks>
/// This provider creates reasonably-sized JPEG thumbnails from source images for Photos and Pictures libraries.
/// It resizes images to a maximum dimension (default 800px) while preserving aspect ratio and
/// converts to a standard JPEG format for consistent display.
/// </remarks>
public partial class LibVipsImageProvider : IImageProvider<ImageMetadata>
{
    /// <summary>
    /// Maximum dimension (width or height) for generated thumbnails.
    /// </summary>
    private const int MaxThumbnailDimension = 800;

    /// <summary>
    /// Maximum decoded pixels allowed to avoid OOM on extremely large images.
    /// </summary>
    private const long MaxDecodedPixels = 120_000_000; // ~12k x 10k

    /// <summary>
    /// JPEG quality for thumbnail encoding.
    /// </summary>
    private const int ThumbnailQuality = 85;

    private readonly ILogger<LibVipsImageProvider> logger;
    private readonly IImageService imageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibVipsImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="imageService">The image service.</param>
    public LibVipsImageProvider(ILogger<LibVipsImageProvider> logger, IImageService imageService)
    {
        this.logger = logger;
        this.imageService = imageService;
    }

    /// <inheritdoc />
    public string Name => "LibVips Image Thumbnailer";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public bool Supports(MediaItem item, ImageMetadata metadata) =>
        SupportsInternal(item);

    /// <inheritdoc />
    public Task ProvideAsync(
        MediaItem item,
        ImageMetadata metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.ProvideInternalAsync(item, metadata, parts, cancellationToken);

    private static bool SupportsInternal(MediaItem? item)
    {
        if (item == null)
        {
            return false;
        }

        // Check if the first part is an image file
        var firstPart = item.Parts?.FirstOrDefault();
        if (firstPart == null || string.IsNullOrWhiteSpace(firstPart.File))
        {
            return false;
        }

        var ext = Path.GetExtension(firstPart.File);
        return MediaFileExtensions.IsImage(ext);
    }

    /// <summary>
    /// Calculates the scale factor to fit an image within the maximum dimension.
    /// </summary>
    /// <param name="width">Source image width.</param>
    /// <param name="height">Source image height.</param>
    /// <param name="maxDimension">Maximum allowed dimension.</param>
    /// <returns>Scale factor (1.0 or less).</returns>
    private static double CalculateScale(int width, int height, int maxDimension)
    {
        var maxSourceDimension = Math.Max(width, height);
        if (maxSourceDimension <= maxDimension)
        {
            return 1.0;
        }

        return (double)maxDimension / maxSourceDimension;
    }

    /// <summary>
    /// Generates a temporary file path for thumbnail generation.
    /// </summary>
    /// <param name="uuid">The metadata UUID.</param>
    /// <returns>The full path to the temporary file.</returns>
    private static string GetTempThumbnailPath(Guid uuid)
    {
        var tempDir = Path.GetTempPath();
        var fileName = string.Create(
            null,
            stackalloc char[128],
            $"nexa_img_thumb_{uuid:N}.jpg"
        );
        return Path.Combine(tempDir, fileName);
    }

    private async Task ProvideInternalAsync(
        MediaItem item,
        ImageMetadata metadata,
        IReadOnlyList<MediaPart>? parts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();
        this.LogProvideStart(item.Id, metadata.Uuid, parts?.Count ?? 0);

        if (parts == null || parts.Count == 0)
        {
            this.LogNoPartsForThumbnail(item.Id);
            this.LogProvideSkipped(item.Id, "no parts");
            return;
        }

        var firstPart = parts[0];
        if (string.IsNullOrWhiteSpace(firstPart.File) || !File.Exists(firstPart.File))
        {
            this.LogMissingPartFile(item.Id, firstPart.File ?? "<null>");
            this.LogProvideSkipped(item.Id, "missing part file");
            return;
        }

        var ext = Path.GetExtension(firstPart.File);
        if (!MediaFileExtensions.IsImage(ext))
        {
            this.LogSkipNonImage(item.Id, ext);
            this.LogProvideSkipped(item.Id, "not image");
            return;
        }

        await this.GenerateImageThumbnailAsync(item, metadata, firstPart, cancellationToken)
            .ConfigureAwait(false);
        this.LogProvideComplete(item.Id);
    }

    private async Task GenerateImageThumbnailAsync(
        MediaItem item,
        ImageMetadata metadata,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.LogGenerateImageStart(item.Id, part.File);

        var tempPath = GetTempThumbnailPath(metadata.Uuid);
        try
        {
            var loadOptions = new VOption { { "autorotate", true } };

            using var sourceImage = NetVips.Image.NewFromFile(
                part.File,
                access: NetVips.Enums.Access.Sequential,
                failOn: NetVips.Enums.FailOn.Warning,
                revalidate: true,
                kwargs: loadOptions
            );

            var totalPixels = (long)sourceImage.Width * sourceImage.Height;
            if (totalPixels > MaxDecodedPixels)
            {
                this.LogSkipImageTooLarge(item.Id, sourceImage.Width, sourceImage.Height, MaxDecodedPixels);
                this.LogProvideSkipped(item.Id, "image too large");
                return;
            }

            // Calculate scaling factor to fit within MaxThumbnailDimension
            var scale = CalculateScale(sourceImage.Width, sourceImage.Height, MaxThumbnailDimension);
            this.LogImageDimensions(item.Id, sourceImage.Width, sourceImage.Height, scale);

            NetVips.Image thumbnail;
            if (scale < 1.0)
            {
                thumbnail = sourceImage.Resize(scale);
            }
            else
            {
                thumbnail = sourceImage;
            }

            try
            {
                // Create save options for JPEG output with metadata stripping
                var saveOptions = new VOption
                {
                    { "Q", ThumbnailQuality },
                    { "strip", true },
                };

                // Handle images with alpha channel by flattening to white background
                if (thumbnail.HasAlpha())
                {
                    using var flattened = thumbnail.Flatten(background: new double[] { 255, 255, 255 });
                    flattened.WriteToFile(tempPath, saveOptions);
                }
                else
                {
                    thumbnail.WriteToFile(tempPath, saveOptions);
                }
            }
            finally
            {
                if (!ReferenceEquals(thumbnail, sourceImage))
                {
                    thumbnail.Dispose();
                }
            }

            if (!File.Exists(tempPath))
            {
                this.LogThumbnailGenerationFailed(item.Id);
                return;
            }

            this.LogGeneratedThumbnail(item.Id, tempPath);

            var bytes = await File.ReadAllBytesAsync(tempPath, cancellationToken)
                .ConfigureAwait(false);
            var thumbUri = await this.imageService.SaveThumbnailAsync(
                metadata.Uuid,
                this.Name,
                bytes,
                "jpg",
                cancellationToken
            );

            // Set ThumbUri immediately if not already populated
            metadata.ThumbUri ??= thumbUri;
            this.LogThumbnailSaved(metadata.Uuid, thumbUri);
            this.LogThumbnailSuccessSummary(item.Id, thumbUri);
        }
        catch (VipsException ex)
        {
            this.LogVipsError(ex, item.Id);
            this.LogThumbnailGenerationFailed(item.Id);
        }
        catch (Exception ex)
        {
            this.LogGeneralError(ex, item.Id);
            this.LogThumbnailGenerationFailed(item.Id);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "LibVips provider start: mediaItem={MediaItemId} metadata={MetadataUuid} parts={PartCount}"
    )]
    private partial void LogProvideStart(int MediaItemId, Guid MetadataUuid, int PartCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "LibVips provider skipped: mediaItem={MediaItemId} reason={Reason}"
    )]
    private partial void LogProvideSkipped(int MediaItemId, string Reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "LibVips provider complete: mediaItem={MediaItemId}"
    )]
    private partial void LogProvideComplete(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: image too large for media item {MediaItemId} width={Width} height={Height} maxPixels={MaxPixels}"
    )]
    private partial void LogSkipImageTooLarge(int MediaItemId, int Width, int Height, long MaxPixels);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: no parts for media item {MediaItemId}"
    )]
    private partial void LogNoPartsForThumbnail(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: part file missing for media item {MediaItemId} path={Path}"
    )]
    private partial void LogMissingPartFile(int MediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail skip: non-image media item {MediaItemId} ext={Extension}"
    )]
    private partial void LogSkipNonImage(int MediaItemId, string? Extension);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GenerateImage start: mediaItem={MediaItemId} path={Path}"
    )]
    private partial void LogGenerateImageStart(int MediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image dimensions: mediaItem={MediaItemId} width={Width} height={Height} scale={Scale}"
    )]
    private partial void LogImageDimensions(int MediaItemId, int Width, int Height, double Scale);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generated thumbnail: item={MediaItemId} tempPath={TempPath}"
    )]
    private partial void LogGeneratedThumbnail(int MediaItemId, string TempPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved thumbnail for metadata {MetadataUuid} via LibVips provider uri={Uri}"
    )]
    private partial void LogThumbnailSaved(Guid MetadataUuid, string Uri);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to generate thumbnail for item={MediaItemId}"
    )]
    private partial void LogThumbnailGenerationFailed(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thumbnail success summary: mediaItem={MediaItemId} thumbUri={ThumbUri}"
    )]
    private partial void LogThumbnailSuccessSummary(int MediaItemId, string ThumbUri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LibVips error processing item {MediaItemId}")]
    private partial void LogVipsError(Exception ex, int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "General error processing thumbnail for item {MediaItemId}"
    )]
    private partial void LogGeneralError(Exception ex, int MediaItemId);

    #endregion
}
