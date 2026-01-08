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
/// Resolves picture items in Pictures libraries. Creates PictureSets for folders containing images.
/// </summary>
/// <remarks>
/// <para>
/// This resolver handles Pictures libraries which typically contain digital art, wallpapers,
/// illustrations, and other visual content that may not represent real-world imagery.
/// </para>
/// <para>
/// Supported folder structures:
/// <list type="bullet">
///   <item><description>Flat: <c>Pictures/wallpaper.jpg</c>.</description></item>
///   <item><description>Categorized: <c>Pictures/Nature/mountain.jpg</c>.</description></item>
///   <item><description>Collections: <c>Pictures/Art Collection/piece1.png</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PictureResolver : ItemResolverBase<MetadataBaseItem>
{
    /// <inheritdoc />
    public override string Name => nameof(PictureResolver);

    /// <inheritdoc />
    public override int Priority => 10;

    /// <inheritdoc />
    public override MetadataBaseItem? Resolve(ItemResolveArgs args)
    {
        // Only operate within Pictures libraries
        if (args.LibraryType != LibraryType.Pictures)
        {
            return null;
        }

        // Only handle directories
        if (!args.File.IsDirectory)
        {
            // Individual image files should be materialized by their parent folder
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

        // If this folder only has subfolders (no images), don't create a set
        // Let subfolders be resolved as individual PictureSets
        if (imageFiles.Count == 0 && subFolders.Count > 0)
        {
            return null;
        }

        var pictureSet = CreatePictureSet(args, imageFiles);

        AttachToParent(pictureSet, args.ResolvedParent);

        return pictureSet;
    }

    private static bool IsImageFile(FileSystemMetadata file) =>
        MediaFileExtensions.IsImage(file.Extension);

    private static PictureSet CreatePictureSet(
        ItemResolveArgs args,
        IReadOnlyList<FileSystemMetadata> imageFiles
    )
    {
        var setTitle = args.File.Name;

        var pictureSet = new PictureSet
        {
            Title = setTitle,
            SortTitle = setTitle,
            LibrarySectionId = args.LibrarySectionId,
        };

        var index = 1;
        foreach (var imageFile in imageFiles)
        {
            var picture = CreatePicture(args, imageFile, index++);
            picture.Parent = pictureSet;
            pictureSet.Children.Add(picture);
        }

        return pictureSet;
    }

    private static void AttachToParent(PictureSet pictureSet, MetadataBaseItem? parent)
    {
        if (parent is not PictureSet parentSet)
        {
            return;
        }

        pictureSet.Parent = parentSet;
        parentSet.Children.Add(pictureSet);
    }

    private static Picture CreatePicture(ItemResolveArgs args, FileSystemMetadata file, int index)
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

        return new Picture
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
