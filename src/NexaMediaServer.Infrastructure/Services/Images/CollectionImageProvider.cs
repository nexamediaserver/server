// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NetVips;

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

using VipsImage = NetVips.Image;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Image provider for collection metadata types that creates a 2x2 collage thumbnail.
/// </summary>
/// <remarks>
/// This provider generates thumbnails for collection types by finding the first 4 child items
/// and creating a 2x2 grid collage of their thumbnails. Collections without
/// children or where children lack thumbnails are skipped.
/// Supports: PhotoAlbum, PictureSet, BookSeries, GameFranchise, GameSeries, Collection, Playlist.
/// </remarks>
public partial class CollectionImageProvider : IImageProvider<MetadataCollectionItem>
{
    /// <summary>
    /// Size of each thumbnail in the collage grid (both width and height).
    /// </summary>
    private const int ThumbnailSize = 400;

    /// <summary>
    /// Maximum number of thumbnails to include in the collage (2x2 grid).
    /// </summary>
    private const int MaxThumbnails = 4;

    /// <summary>
    /// JPEG quality for collage encoding.
    /// </summary>
    private const int CollageQuality = 85;

    /// <summary>
    /// Maps parent collection MetadataTypes to their expected child MetadataTypes.
    /// </summary>
    private static readonly FrozenDictionary<MetadataType, MetadataType[]> ParentToChildTypeMapping =
        new Dictionary<MetadataType, MetadataType[]>
        {
            [MetadataType.PhotoAlbum] = [MetadataType.Photo],
            [MetadataType.PictureSet] = [MetadataType.Picture],
            [MetadataType.BookSeries] = [MetadataType.EditionGroup, MetadataType.Edition],
            [MetadataType.GameFranchise] = [MetadataType.GameSeries, MetadataType.Game],
            [MetadataType.GameSeries] = [MetadataType.Game],
            [MetadataType.Collection] = [MetadataType.Movie, MetadataType.Show, MetadataType.Episode, MetadataType.AlbumRelease],
            [MetadataType.Playlist] = [MetadataType.Movie, MetadataType.Episode, MetadataType.Track],
        }.ToFrozenDictionary();

    private readonly ILogger<CollectionImageProvider> logger;
    private readonly IImageService imageService;
    private readonly IServiceScopeFactory serviceScopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="imageService">The image service.</param>
    /// <param name="serviceScopeFactory">Factory to create service scopes for accessing scoped repositories.</param>
    public CollectionImageProvider(
        ILogger<CollectionImageProvider> logger,
        IImageService imageService,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        this.logger = logger;
        this.imageService = imageService;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public string Name => "Collection Collage Generator";

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public bool Supports(MediaItem item, MetadataCollectionItem metadata) =>
        metadata != null;

    /// <inheritdoc />
    public Task ProvideAsync(
        MediaItem item,
        MetadataCollectionItem metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.ProvideInternalAsync(item, metadata, cancellationToken);

    /// <summary>
    /// Resizes a thumbnail to fit within the target size while maintaining aspect ratio.
    /// </summary>
    /// <param name="image">Source image.</param>
    /// <param name="targetSize">Target size (both width and height).</param>
    /// <returns>Resized image cropped to square.</returns>
    private static VipsImage ResizeThumbnail(VipsImage image, int targetSize)
    {
        // Calculate scale to cover the target size
        var scale = Math.Max(
            (double)targetSize / image.Width,
            (double)targetSize / image.Height
        );

        var scaled = image.Resize(scale);

        // Crop to square from center
        var left = Math.Max(0, (scaled.Width - targetSize) / 2);
        var top = Math.Max(0, (scaled.Height - targetSize) / 2);
        var cropped = scaled.Crop(left, top, targetSize, targetSize);

        if (!ReferenceEquals(cropped, scaled))
        {
            scaled.Dispose();
        }

        return cropped;
    }

    /// <summary>
    /// Creates a 2x2 grid from 1-4 images.
    /// Images are placed left-to-right, top-to-bottom (top-left, top-right, bottom-left, bottom-right).
    /// </summary>
    /// <param name="images">List of images (1-4 items, all same size).</param>
    /// <returns>Composite grid image.</returns>
    private static VipsImage CreateGrid(List<VipsImage> images)
    {
        var count = images.Count;
        var size = ThumbnailSize;

        if (count == 1)
        {
            // Single image - return as is
            return images[0];
        }

        if (count == 2)
        {
            // Two images - side by side
            return images[0].Join(images[1], NetVips.Enums.Direction.Horizontal);
        }

        if (count == 3)
        {
            // Three images - first two on top, third in bottom-left (top-left aligned)
            var topRow = images[0].Join(images[1], NetVips.Enums.Direction.Horizontal);
            var bottomRow = VipsImage.Black(size * 2, size);
            bottomRow = bottomRow.Insert(images[2], 0, 0); // Left-aligned (x=0)
            var result = topRow.Join(bottomRow, NetVips.Enums.Direction.Vertical);
            topRow.Dispose();
            bottomRow.Dispose();
            return result;
        }

        // Four images - 2x2 grid
        var topRowFull = images[0].Join(images[1], NetVips.Enums.Direction.Horizontal);
        var bottomRowFull = images[2].Join(images[3], NetVips.Enums.Direction.Horizontal);
        var grid = topRowFull.Join(bottomRowFull, NetVips.Enums.Direction.Vertical);
        topRowFull.Dispose();
        bottomRowFull.Dispose();
        return grid;
    }

    private async Task ProvideInternalAsync(
        MediaItem item,
        MetadataCollectionItem metadata,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();

        this.LogProvideStart(metadata.Uuid, metadata.GetType().Name);

        // Get the parent's MetadataType from the entity to determine child types
        using var scope = this.serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMetadataItemRepository>();

        // Find the parent metadata item entity to get its Id and MetadataType
        var parent = await repository
            .GetQueryable()
            .Where(m => m.Uuid == metadata.Uuid)
            .Select(m => new { m.Id, m.MetadataType })
            .FirstOrDefaultAsync(cancellationToken);

        if (parent == null)
        {
            this.LogParentNotFound(metadata.Uuid);
            this.LogProvideSkipped(metadata.Uuid, "parent not found");
            return;
        }

        // Get expected child types for this collection type
        if (!ParentToChildTypeMapping.TryGetValue(parent.MetadataType, out var childTypes))
        {
            this.LogProvideSkipped(metadata.Uuid, $"no child type mapping for {parent.MetadataType}");
            return;
        }

        // Find the first 4 child items with thumbnails (matching any of the expected child types)
        var children = await repository
            .GetQueryable()
            .Where(m => m.ParentId == parent.Id && childTypes.Contains(m.MetadataType) && m.ThumbUri != null)
            .OrderBy(m => m.Index)
                .ThenBy(m => m.Id)
            .Select(m => new { m.ThumbUri })
            .Take(MaxThumbnails)
            .ToListAsync(cancellationToken);

        if (children.Count == 0)
        {
            this.LogNoChildrenFound(metadata.Uuid);
            this.LogProvideSkipped(metadata.Uuid, "no children with thumbnails");
            return;
        }

        this.LogFoundChildren(metadata.Uuid, children.Count);

        // Create collage from child thumbnails
        var collageBytes = await this.CreateCollageAsync(
            children.Select(c => c.ThumbUri!).ToList(),
            cancellationToken
        );

        if (collageBytes == null)
        {
            this.LogCollageCreationFailed(metadata.Uuid);
            this.LogProvideSkipped(metadata.Uuid, "collage creation failed");
            return;
        }

        // Save the collage as the collection's thumbnail
        var thumbUri = await this.imageService.SaveThumbnailAsync(
            metadata.Uuid,
            this.Name,
            collageBytes,
            "jpg",
            cancellationToken
        );

        metadata.ThumbUri ??= thumbUri;
        this.LogCollageSaved(metadata.Uuid, thumbUri, children.Count);
        this.LogProvideComplete(metadata.Uuid);
    }

    /// <summary>
    /// Creates a 2x2 collage from child item thumbnails.
    /// </summary>
    /// <param name="thumbUris">List of thumbnail URIs (1-4 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG bytes of the collage, or null if creation fails.</returns>
    private async Task<byte[]?> CreateCollageAsync(
        List<string> thumbUris,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Use transcode service to get local file paths for each thumbnail
            var thumbnailPaths = new List<string>();
            foreach (var uri in thumbUris)
            {
                var request = new ImageTranscodeRequest { SourceUri = uri };
                var path = await this.imageService.GetOrTranscodeAsync(request, cancellationToken);
                if (path != null && File.Exists(path))
                {
                    thumbnailPaths.Add(path);
                }
            }

            if (thumbnailPaths.Count == 0)
            {
                return null;
            }

            // Load and resize all thumbnails to consistent size
            var resizedImages = new List<VipsImage>();
            foreach (var path in thumbnailPaths)
            {
                var img = VipsImage.NewFromFile(path, access: NetVips.Enums.Access.Sequential);
                var resized = ResizeThumbnail(img, ThumbnailSize);
                if (!ReferenceEquals(resized, img))
                {
                    img.Dispose();
                }

                resizedImages.Add(resized);
            }

            // Create the collage grid
            var collage = CreateGrid(resizedImages);

            // Encode to JPEG
            var saveOptions = new VOption { { "Q", CollageQuality }, { "strip", true } };
            var bytes = collage.WriteToBuffer(".jpg", saveOptions);
            collage.Dispose();

            // Cleanup resized images
            foreach (var img in resizedImages)
            {
                img.Dispose();
            }

            return bytes;
        }
        catch (VipsException ex)
        {
            this.LogVipsError(ex);
            return null;
        }
        catch (Exception ex)
        {
            this.LogGeneralError(ex);
            return null;
        }
    }

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Collection provider start: metadata={MetadataUuid} type={MetadataType}"
    )]
    private partial void LogProvideStart(Guid MetadataUuid, string MetadataType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Collection provider skipped: metadata={MetadataUuid} reason={Reason}"
    )]
    private partial void LogProvideSkipped(Guid MetadataUuid, string Reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Parent metadata item not found: metadata={MetadataUuid}"
    )]
    private partial void LogParentNotFound(Guid MetadataUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Collection provider complete: metadata={MetadataUuid}"
    )]
    private partial void LogProvideComplete(Guid MetadataUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No children with thumbnails found for collection metadata={MetadataUuid}"
    )]
    private partial void LogNoChildrenFound(Guid MetadataUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {Count} children with thumbnails for collection metadata={MetadataUuid}"
    )]
    private partial void LogFoundChildren(Guid MetadataUuid, int Count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to create collage for collection metadata={MetadataUuid}"
    )]
    private partial void LogCollageCreationFailed(Guid MetadataUuid);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saved collage thumbnail: collection={MetadataUuid} uri={ThumbUri} childCount={ChildCount}"
    )]
    private partial void LogCollageSaved(Guid MetadataUuid, string ThumbUri, int ChildCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LibVips error creating collage")]
    private partial void LogVipsError(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "General error creating collage")]
    private partial void LogGeneralError(Exception ex);

    #endregion
}
