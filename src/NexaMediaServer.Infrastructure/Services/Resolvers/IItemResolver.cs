// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves a scanned filesystem path into a domain <see cref="MetadataItem"/>.
///
/// Inspired by Jellyfin's IItemResolver pipeline.
/// </summary>
public interface IItemResolver
{
    /// <summary>
    /// Gets the priority ordering for this resolver.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Resolve the path into a MetadataItem skeleton or return null if not applicable.
    /// </summary>
    /// <param name="args">Resolution arguments from the scan context.</param>
    /// <returns>Resolved MetadataItem or null.</returns>
    MetadataItem? Resolve(ItemResolveArgs args);
}

/// <summary>
/// Represents basic filesystem metadata for a path used in resolution.
/// </summary>
/// <param name="Exists">Whether the path exists.</param>
/// <param name="Path">Absolute path.</param>
/// <param name="Name">File or directory name.</param>
/// <param name="Extension">File extension (empty for directories).</param>
/// <param name="LastModifiedTimeUtc">UTC last write time or null if unavailable.</param>
/// <param name="CreationTimeUtc">UTC creation time or null if unavailable.</param>
/// <param name="IsDirectory">True when path is a directory.</param>
public readonly record struct FileSystemMetadata(
    bool Exists,
    string Path,
    string Name,
    string Extension,
    DateTime? LastModifiedTimeUtc,
    DateTime? CreationTimeUtc,
    bool IsDirectory
)
{
    /// <summary>
    /// Creates metadata from a filesystem path (best-effort, tolerates missing path). IO exceptions are swallowed because metadata is optional.
    /// </summary>
    /// <param name="path">Absolute or relative path.</param>
    /// <returns>Populated <see cref="FileSystemMetadata"/>.</returns>
    public static FileSystemMetadata FromPath(string path)
    {
        try
        {
            var exists = File.Exists(path) || System.IO.Directory.Exists(path);
            var isDir = System.IO.Directory.Exists(path);
            var name = System.IO.Path.GetFileName(path);
            var ext = isDir ? string.Empty : System.IO.Path.GetExtension(path) ?? string.Empty;
            DateTime? lm = null;
            DateTime? ct = null;
            if (exists)
            {
                try
                {
                    lm = isDir
                        ? System.IO.Directory.GetLastWriteTimeUtc(path)
                        : File.GetLastWriteTimeUtc(path);
                }
                catch
                {
                    // Ignored: last write time not critical
                }

                try
                {
                    ct = isDir
                        ? System.IO.Directory.GetCreationTimeUtc(path)
                        : File.GetCreationTimeUtc(path);
                }
                catch
                {
                    // Ignored: creation time not critical
                }
            }

            return new FileSystemMetadata(exists, path, name, ext, lm, ct, isDir);
        }
        catch
        {
            return new FileSystemMetadata(
                false,
                path,
                System.IO.Path.GetFileName(path),
                string.Empty,
                null,
                null,
                false
            );
        }
    }
}

/// <summary>
/// Immutable arguments for a single resolve attempt.
/// </summary>
/// <param name="File">Filesystem metadata for current path.</param>
/// <param name="LibraryType">Library type context.</param>
/// <param name="SectionLocationId">Location id in library.</param>
/// <param name="LibrarySectionId">Library section id.</param>
/// <param name="FileSystemChildren">Optional pre-fetched child entries (names only) for directory-aware resolvers.</param>
/// <param name="IsRoot">Whether the current path is the root of the library section.</param>
public readonly record struct ItemResolveArgs(
    FileSystemMetadata File,
    LibraryType LibraryType,
    int SectionLocationId,
    int LibrarySectionId,
    IReadOnlyList<FileSystemMetadata>? FileSystemChildren,
    bool IsRoot
);

/// <summary>
/// Convenience base class providing default priority and null resolution.
/// </summary>
public abstract class ItemResolverBase : IItemResolver
{
    /// <inheritdoc />
    public virtual int Priority => 0;

    /// <inheritdoc />
    public virtual MetadataItem? Resolve(ItemResolveArgs args) => null;
}
