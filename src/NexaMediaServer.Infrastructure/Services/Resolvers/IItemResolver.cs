// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves a scanned filesystem path into a domain <see cref="MetadataBaseItem"/>.
///
/// Inspired by Jellyfin's IItemResolver pipeline.
/// </summary>
/// <typeparam name="TMetadata">The metadata DTO type handled by this resolver.</typeparam>
public interface IItemResolver<out TMetadata>
    where TMetadata : MetadataBaseItem
{
    /// <summary>
    /// Gets a unique identifier for this resolver to use for logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority ordering for this resolver.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Resolve the path into a MetadataItem skeleton or return null if not applicable.
    /// </summary>
    /// <param name="args">Resolution arguments from the scan context.</param>
    /// <returns>Resolved MetadataItem or null.</returns>
    TMetadata? Resolve(ItemResolveArgs args);
}

/// <summary>
/// Represents basic filesystem metadata for a path used in resolution.
/// </summary>
/// <param name="Exists">Whether the path exists.</param>
/// <param name="Path">Absolute path.</param>
/// <param name="Name">File or directory name.</param>
/// <param name="Extension">File extension (empty for directories).</param>
/// <param name="Size">File size in bytes, or null if unavailable or if path is a directory.</param>
/// <param name="LastModifiedTimeUtc">UTC last write time or null if unavailable.</param>
/// <param name="CreationTimeUtc">UTC creation time or null if unavailable.</param>
/// <param name="IsDirectory">True when path is a directory.</param>
public readonly record struct FileSystemMetadata(
    bool Exists,
    string Path,
    string Name,
    string Extension,
    long? Size,
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

            if (!exists)
            {
                return new FileSystemMetadata(false, path, name, ext, null, null, null, isDir);
            }

            var (size, lm, ct) = GetFileStats(path, isDir);
            return new FileSystemMetadata(exists, path, name, ext, size, lm, ct, isDir);
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
                null,
                false
            );
        }
    }

    private static (long? Size, DateTime? LastModified, DateTime? Created) GetFileStats(
        string path,
        bool isDirectory
    )
    {
        long? size = null;
        DateTime? lm = null;
        DateTime? ct = null;

        if (!isDirectory)
        {
            try
            {
                size = new FileInfo(path).Length;
            }
            catch
            {
                // Ignored: file size not critical
            }
        }

        try
        {
            lm = isDirectory
                ? System.IO.Directory.GetLastWriteTimeUtc(path)
                : File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            // Ignored: last write time not critical
        }

        try
        {
            ct = isDirectory
                ? System.IO.Directory.GetCreationTimeUtc(path)
                : File.GetCreationTimeUtc(path);
        }
        catch
        {
            // Ignored: creation time not critical
        }

        return (size, lm, ct);
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
/// <param name="Ancestors">Optional chain of ancestors from root to parent, including any resolved metadata.</param>
/// <param name="ResolvedParent">Resolved metadata item for the immediate parent, if known.</param>
/// <param name="Siblings">Optional sibling entries to inform grouping heuristics.</param>
public readonly record struct ItemResolveArgs(
    FileSystemMetadata File,
    LibraryType LibraryType,
    int SectionLocationId,
    int LibrarySectionId,
    IReadOnlyList<FileSystemMetadata>? FileSystemChildren,
    bool IsRoot,
    IReadOnlyList<AncestorInfo>? Ancestors,
    MetadataBaseItem? ResolvedParent,
    IReadOnlyList<FileSystemMetadata>? Siblings
);

/// <summary>
/// Represents an ancestor in the filesystem chain for hierarchical resolution.
/// </summary>
/// <param name="Path">Absolute path of the ancestor.</param>
/// <param name="File">Filesystem metadata of the ancestor.</param>
/// <param name="ResolvedItem">Resolved metadata for the ancestor, if already identified.</param>
public readonly record struct AncestorInfo(
    string Path,
    FileSystemMetadata File,
    MetadataBaseItem? ResolvedItem
);

/// <summary>
/// Convenience base class providing default priority and null resolution.
/// </summary>
/// <typeparam name="TMetadata">The metadata DTO type handled by this resolver.</typeparam>
public abstract class ItemResolverBase<TMetadata> : IItemResolver<TMetadata>
    where TMetadata : MetadataBaseItem
{
    /// <inheritdoc />
    public virtual string Name => this.GetType().Name;

    /// <inheritdoc />
    public virtual int Priority => 0;

    /// <inheritdoc />
    public virtual TMetadata? Resolve(ItemResolveArgs args) => null;
}
