// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;

using Microsoft.EntityFrameworkCore;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// Repository for managing library folder entities.
/// </summary>
public class SectionLocationRepository : ISectionLocationRepository
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly char[] DirectorySeparators =
    [
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    ];

    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionLocationRepository"/> class.
    /// </summary>
    /// <param name="context">The media server database context.</param>
    public SectionLocationRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public IQueryable<SectionLocation> GetQueryable()
    {
        return this.context.SectionsLocations.AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<SectionLocation> AddAsync(SectionLocation folder)
    {
        folder.CreatedAt = DateTime.UtcNow;

        this.context.SectionsLocations.Add(folder);
        await this.context.SaveChangesAsync();

        return folder;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var folder = await this.context.SectionsLocations.FindAsync(id);
        if (folder != null)
        {
            this.context.SectionsLocations.Remove(folder);
            await this.context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public Task<SectionLocation?> GetByIdAsync(int id)
    {
        return this.context.SectionsLocations.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    }

    /// <inheritdoc />
    public Task<List<SectionLocation>> GetByLibraryIdAsync(int libraryId)
    {
        return this
            .context.SectionsLocations.Where(f => f.LibrarySectionId == libraryId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> PathExistsAsync(string path, int? excludeLibraryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Canonicalize the input path to resolve relative paths, symlinks, and ensure consistency.
        string normalizedPath = NormalizePath(path);

        var query = this.context.SectionsLocations.AsNoTracking();
        if (excludeLibraryId.HasValue)
        {
            query = query.Where(f => f.LibrarySectionId != excludeLibraryId.Value);
        }

        var existingPaths = await query.Select(f => f.RootPath).ToListAsync();

        return existingPaths.Any(existingPath =>
        {
            string normalizedExisting = NormalizePath(existingPath);
            return PathsOverlap(normalizedExisting, normalizedPath);
        });
    }

    /// <summary>
    /// Normalizes a path for consistent comparison across platforms.
    /// Resolves relative paths, removes trailing separators, and applies platform-specific casing.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    private static string NormalizePath(string path)
    {
        string trimmed = path.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        // Resolve to absolute path and canonicalize (handles .., ., symlinks, etc.)
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(trimmed);
        }
        catch (Exception)
        {
            // Invalid path - return as-is for comparison to fail naturally
            return trimmed;
        }

        // Handle Windows-specific root paths
        if (IsWindows && (IsDriveRoot(fullPath) || IsUncRoot(fullPath)))
        {
            return EnsureTrailingSeparator(fullPath);
        }

        // Unix root path
        if (fullPath == "/")
        {
            return "/";
        }

        // Remove trailing separators for non-root paths
        string withoutTrailing = fullPath.TrimEnd(DirectorySeparators);

        // Windows paths are case-insensitive - normalize to lowercase for comparison
        return IsWindows ? withoutTrailing.ToLowerInvariant() : withoutTrailing;
    }

    /// <summary>
    /// Determines whether two paths overlap (exact match or one contains the other).
    /// </summary>
    /// <param name="existingPath">An existing library root path.</param>
    /// <param name="candidatePath">The candidate path to check.</param>
    /// <returns>True if the paths overlap; otherwise false.</returns>
    private static bool PathsOverlap(string existingPath, string candidatePath)
    {
        // Since NormalizePath already lowercased Windows paths, use Ordinal comparison
        var comparison = StringComparison.Ordinal;

        if (existingPath.Length == 0 || candidatePath.Length == 0)
        {
            return false;
        }

        if (existingPath.Equals(candidatePath, comparison))
        {
            return true;
        }

        string existingWithSeparator = EnsureTrailingSeparator(existingPath);
        string candidateWithSeparator = EnsureTrailingSeparator(candidatePath);

        // Treat nested paths as duplicates to avoid scanning the same tree twice.
        return candidateWithSeparator.StartsWith(existingWithSeparator, comparison)
            || existingWithSeparator.StartsWith(candidateWithSeparator, comparison);
    }

    /// <summary>
    /// Ensures a path ends with a directory separator.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>The path with a trailing separator.</returns>
    private static string EnsureTrailingSeparator(string path)
    {
        if (path.Length == 0)
        {
            return path;
        }

        char last = path[^1];
        if (IsDirectorySeparator(last))
        {
            return path;
        }

        // Unix root is a special case
        if (path == "/")
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Checks whether a path represents a Windows drive root (e.g., C:\).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a drive root; otherwise false.</returns>
    private static bool IsDriveRoot(string path)
    {
        if (path.Length < 2)
        {
            return false;
        }

        // Check for pattern like "C:" or "C:\"
        if (char.IsLetter(path[0]) && path[1] == ':')
        {
            return path.Length == 2 || (path.Length == 3 && IsDirectorySeparator(path[2]));
        }

        return false;
    }

    /// <summary>
    /// Checks whether a path represents a Windows UNC root (e.g., \\server\share).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a UNC root; otherwise false.</returns>
    private static bool IsUncRoot(string path)
    {
        if (path.Length < 5)
        {
            return false;
        }

        // Must start with \\ or //
        if (!(IsDirectorySeparator(path[0]) && IsDirectorySeparator(path[1])))
        {
            return false;
        }

        // Count separators - UNC root has exactly 3: \\server\share
        int separatorCount = 0;
        for (int i = 2; i < path.Length; i++)
        {
            if (IsDirectorySeparator(path[i]))
            {
                separatorCount++;
            }
        }

        // \\server\share or \\server\share\ both qualify as UNC root
        return separatorCount <= 1;
    }

    /// <summary>
    /// Checks whether a character is a directory separator.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is a directory separator; otherwise false.</returns>
    private static bool IsDirectorySeparator(char character)
    {
        return character == Path.DirectorySeparatorChar
            || character == Path.AltDirectorySeparatorChar;
    }
}
