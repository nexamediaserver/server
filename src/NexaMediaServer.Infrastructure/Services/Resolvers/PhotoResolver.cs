// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

using IODir = System.IO.Directory;
using IOPath = System.IO.Path;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves photo items in Photos libraries. Creates PhotoAlbums for folders containing images.
/// </summary>
/// <remarks>
/// <para>
/// This resolver handles Photos libraries which typically contain personal photographs
/// organized by date, event, or location. Photos are expected to be real-world imagery
/// captured using cameras or similar devices.
/// </para>
/// <para>
/// Supported folder structures:
/// <list type="bullet">
///   <item><description>Flat: <c>Photos/IMG_1234.jpg</c>.</description></item>
///   <item><description>Date-based: <c>Photos/2024/01/IMG_1234.jpg</c>.</description></item>
///   <item><description>Event-based: <c>Photos/Vacation 2024/IMG_1234.jpg</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PhotoResolver : ItemResolverBase<PhotoAlbum>
{
    /// <inheritdoc />
    public override string Name => nameof(PhotoResolver);

    /// <inheritdoc />
    public override int Priority => 10;

    /// <inheritdoc />
    public override PhotoAlbum? Resolve(ItemResolveArgs args)
    {
        // Only operate within Photos libraries
        if (args.LibraryType != LibraryType.Photos)
        {
            return null;
        }

        // Only handle directories
        if (!args.File.IsDirectory)
        {
            // Single image file at root - create a photo directly
            if (args.IsRoot)
            {
                return null;
            }

            if (IsImageFile(args.File))
            {
                // Let the parent folder create this as a child
                return null;
            }

            return null;
        }

        // Skip library root
        if (args.IsRoot)
        {
            return null;
        }

        var children = GetChildren(args);

        // Check if this folder contains images
        var imageFiles = children
            .Where(c => !c.IsDirectory && IsImageFile(c))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var subFolders = children
            .Where(c => c.IsDirectory)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // If no images and no subfolders, skip
        if (imageFiles.Count == 0 && subFolders.Count == 0)
        {
            return null;
        }

        // If this folder only has subfolders (no images), it might be a date folder (2024, 01, etc.)
        // In that case, don't create an album - let subfolders be resolved as albums
        if (imageFiles.Count == 0 && subFolders.Count > 0)
        {
            return null;
        }

        return CreatePhotoAlbum(args, imageFiles);
    }

    private static bool IsImageFile(FileSystemMetadata file) =>
        MediaFileExtensions.IsImage(file.Extension);

    private static PhotoAlbum CreatePhotoAlbum(
        ItemResolveArgs args,
        IReadOnlyList<FileSystemMetadata> imageFiles
    )
    {
        var albumTitle = args.File.Name;

        var album = new PhotoAlbum
        {
            Title = albumTitle,
            SortTitle = albumTitle,
            LibrarySectionId = args.LibrarySectionId,
        };

        var index = 1;
        foreach (var imageFile in imageFiles)
        {
            var photo = CreatePhoto(args, imageFile, index++);
            photo.Parent = album;
            album.Children.Add(photo);
        }

        return album;
    }

    private static Photo CreatePhoto(ItemResolveArgs args, FileSystemMetadata file, int index)
    {
        var title = IOPath.GetFileNameWithoutExtension(file.Name);
        var ext = file.Extension?.TrimStart('.').ToLowerInvariant();

        var part = new MediaPart { File = file.Path };
        try
        {
            var fi = new FileInfo(file.Path);
            if (fi.Exists)
            {
                part.Size = fi.Length;
            }
        }
        catch
        {
            // ignore file info errors
        }

        var mediaItem = new MediaItem
        {
            SectionLocationId = args.SectionLocationId,
            FileFormat = string.IsNullOrEmpty(ext) ? null : ext,
            Parts = [part],
        };
        part.MediaItem = mediaItem;

        try
        {
            mediaItem.FileSizeBytes = part.Size;
        }
        catch
        {
            // ignore aggregation errors
        }

        return new Photo
        {
            Title = title,
            SortTitle = title,
            Index = index,
            LibrarySectionId = args.LibrarySectionId,
            MediaItems = [mediaItem],
        };
    }

    private static IReadOnlyList<FileSystemMetadata> GetChildren(ItemResolveArgs args)
    {
        if (args.FileSystemChildren is not null)
        {
            return args.FileSystemChildren;
        }

        try
        {
            var entries = IODir
                .EnumerateFileSystemEntries(args.File.Path)
                .Select(FileSystemMetadata.FromPath)
                .ToList();
            return entries;
        }
        catch
        {
            return Array.Empty<FileSystemMetadata>();
        }
    }
}
