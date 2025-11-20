// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Exceptions;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides secure enumeration of filesystem roots and directory listings for the UI browser.
/// </summary>
public sealed class FileSystemBrowserService : IFileSystemBrowserService
{
    private const int MaxEntriesPerDirectory = 500;
    private static readonly char[] DirectorySeparators = new[]
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };
    private static readonly HashSet<string> DeniedMountTypes = new(
        [
            "proc",
            "sysfs",
            "devpts",
            "devtmpfs",
            "cgroup",
            "cgroup2",
            "pstore",
            "securityfs",
            "tracefs",
            "debugfs",
            "configfs",
            "hugetlbfs",
            "fusectl",
            "mqueue",
            "binfmt_misc",
            "ramfs",
            "autofs",
        ],
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly string[] DeniedPathPrefixes =
    [
        "/proc",
        "/sys",
        "/dev",
        "/run",
        "/var/run/secrets",
        "/tmp",
        "/var/tmp",
    ];

    private static readonly Action<ILogger, string, Exception?> DirectoryMissingLog =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1000, nameof(DirectoryMissingLog)),
            "Directory {DirectoryPath} does not exist when browsing."
        );

    private static readonly Action<ILogger, string, Exception?> SymbolicLinkBlockedLog =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1001, nameof(SymbolicLinkBlockedLog)),
            "Blocked attempt to browse symbolic link at {DirectoryPath}."
        );

    private static readonly Action<ILogger, string, Exception?> DirectoryAccessDeniedLog =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1002, nameof(DirectoryAccessDeniedLog)),
            "Access denied when enumerating directory {DirectoryPath}."
        );

    private static readonly Action<ILogger, string, Exception?> DirectoryIoFailureLog =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1003, nameof(DirectoryIoFailureLog)),
            "IO failure when enumerating directory {DirectoryPath}."
        );

    private readonly ILogger<FileSystemBrowserService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBrowserService"/> class.
    /// </summary>
    /// <param name="logger">The logger used for diagnostic output.</param>
    public FileSystemBrowserService(ILogger<FileSystemBrowserService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileSystemRootDto>> GetRootsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<FileSystemRootDto> roots = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsRoots()
            : GetUnixRoots();

        return Task.FromResult(roots);
    }

    /// <inheritdoc />
    public async Task<DirectoryListingDto> GetDirectoryAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileSystemBrowserException("A path must be provided.");
        }

        var canonicalPath = CanonicalizePath(path);
        IReadOnlyList<FileSystemRootDto> roots = await this.GetRootsAsync(cancellationToken);
        var root = roots.FirstOrDefault(r => IsPathWithinRoot(canonicalPath, r.Path));
        if (root is null)
        {
            throw new FileSystemBrowserException(
                "The requested path is outside the allowed filesystem roots."
            );
        }

        if (!Directory.Exists(canonicalPath))
        {
            DirectoryMissingLog(this.logger, canonicalPath, null);
            throw new FileSystemBrowserException(
                "The requested directory does not exist or cannot be accessed."
            );
        }

        var directoryInfo = new DirectoryInfo(canonicalPath);
        if (directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            SymbolicLinkBlockedLog(this.logger, canonicalPath, null);
            throw new FileSystemBrowserException(
                "Symbolic links cannot be browsed for security reasons."
            );
        }

        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        List<FileSystemEntryDto> entries = new();
        try
        {
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(canonicalPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entryPath);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }

                bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
                bool isSymlink = attributes.HasFlag(FileAttributes.ReparsePoint);
                var entry = new FileSystemEntryDto
                {
                    Name = GetEntryName(entryPath),
                    Path = entryPath,
                    IsDirectory = isDirectory,
                    IsFile = !isDirectory,
                    IsSymbolicLink = isSymlink,
                    IsSelectable = isDirectory && !isSymlink,
                };
                entries.Add(entry);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            DirectoryAccessDeniedLog(this.logger, canonicalPath, ex);
            throw new FileSystemBrowserException(
                "Access to the requested directory was denied.",
                ex
            );
        }
        catch (IOException ex)
        {
            DirectoryIoFailureLog(this.logger, canonicalPath, ex);
            throw new FileSystemBrowserException(
                "Unable to enumerate the requested directory.",
                ex
            );
        }

        entries = entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name, comparer)
            .Take(MaxEntriesPerDirectory)
            .ToList();

        var parentPath = GetParentPath(canonicalPath, root.Path);

        return new DirectoryListingDto
        {
            CurrentPath = canonicalPath,
            ParentPath = parentPath,
            Entries = entries,
        };
    }

    private static List<FileSystemRootDto> GetWindowsRoots()
    {
        var drives = DriveInfo.GetDrives();
        var results = new List<FileSystemRootDto>();
        foreach (var drive in drives)
        {
            if (!drive.IsReady && drive.DriveType == DriveType.Removable)
            {
                continue;
            }

            var path = drive.Name;
            var label = drive.Name;
            results.Add(
                new FileSystemRootDto
                {
                    Id = CreateRootId(path),
                    Label = label,
                    Path = path,
                    Kind = FileSystemRootKind.Drive,
                    IsReadOnly = drive.DriveType == DriveType.CDRom,
                }
            );
        }

        return results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<FileSystemRootDto> GetUnixRoots()
    {
        var roots = new List<FileSystemRootDto>
        {
            new()
            {
                Id = CreateRootId("/"),
                Label = "/",
                Path = "/",
                Kind = FileSystemRootKind.Root,
                IsReadOnly = false,
            },
        };

        if (!File.Exists("/proc/mounts"))
        {
            return roots;
        }

        var seenPaths = new HashSet<string>(StringComparer.Ordinal) { "/" };
        foreach (var line in File.ReadLines("/proc/mounts"))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            var mountPoint = UnescapeProcField(parts[1]);
            var fsType = parts[2];
            var flags = parts[3];

            if (!mountPoint.StartsWith('/'))
            {
                continue;
            }

            var canonicalMount = CanonicalizePath(mountPoint);

            if (DeniedMountTypes.Contains(fsType) || IsDeniedPath(canonicalMount))
            {
                continue;
            }

            if (!seenPaths.Add(canonicalMount))
            {
                continue;
            }

            bool isReadOnly = flags
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(f => f == "ro");

            roots.Add(
                new FileSystemRootDto
                {
                    Id = CreateRootId(canonicalMount),
                    Label = canonicalMount,
                    Path = canonicalMount,
                    Kind = FileSystemRootKind.Mount,
                    IsReadOnly = isReadOnly,
                }
            );
        }

        return roots.OrderBy(r => r.Path, StringComparer.Ordinal).ToList();
    }

    private static string CreateRootId(string path)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(path));
    }

    private static bool IsDeniedPath(string path)
    {
        return DeniedPathPrefixes.Any(prefix =>
            path.Equals(prefix, StringComparison.Ordinal)
            || path.StartsWith(prefix + '/', StringComparison.Ordinal)
        );
    }

    private static string CanonicalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (IsDriveRoot(fullPath) || IsUncRoot(fullPath))
            {
                return EnsureTrailingSeparator(fullPath);
            }

            return fullPath.TrimEnd(DirectorySeparators);
        }

        return fullPath == "/" ? "/" : fullPath.TrimEnd('/');
    }

    private static string? GetParentPath(string path, string rootPath)
    {
        if (PathsEqual(path, rootPath))
        {
            return null;
        }

        var parent = Directory.GetParent(path)?.FullName;
        if (parent is null)
        {
            return null;
        }

        var canonicalParent = CanonicalizePath(parent);
        return IsPathWithinRoot(canonicalParent, rootPath) ? canonicalParent : null;
    }

    private static bool IsPathWithinRoot(string candidate, string rootPath)
    {
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (PathsEqual(candidate, rootPath, comparison))
        {
            return true;
        }

        var prefix = EnsureTrailingSeparator(rootPath);
        return candidate.StartsWith(prefix, comparison);
    }

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

        if (path == "/")
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static bool PathsEqual(string left, string right)
    {
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return PathsEqual(left, right, comparison);
    }

    private static bool PathsEqual(string left, string right, StringComparison comparison)
    {
        return string.Equals(CanonicalizePath(left), CanonicalizePath(right), comparison);
    }

    private static bool IsDirectorySeparator(char character)
    {
        return character == Path.DirectorySeparatorChar
            || character == Path.AltDirectorySeparatorChar;
    }

    private static bool IsDriveRoot(string path)
    {
        return path.Length == 3
            && char.IsLetter(path[0])
            && path[1] == ':'
            && IsDirectorySeparator(path[2]);
    }

    private static bool IsUncRoot(string path)
    {
        if (!path.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = path.TrimEnd(DirectorySeparators);
        var segments = trimmed.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 2;
    }

    private static string GetEntryName(string entryPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var trimmed = entryPath.TrimEnd(DirectorySeparators);
            var name = Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? trimmed : name;
        }

        if (entryPath == "/")
        {
            return "/";
        }

        var trimmedPath = entryPath.TrimEnd('/');
        var index = trimmedPath.LastIndexOf('/');
        if (index >= 0)
        {
            return trimmedPath[(index + 1)..];
        }

        return trimmedPath;
    }

    private static string UnescapeProcField(string value)
    {
        var builder = new StringBuilder(value.Length);
        var index = 0;
        while (index < value.Length)
        {
            if (
                value[index] == '\\'
                && index + 3 < value.Length
                && char.IsDigit(value[index + 1])
                && char.IsDigit(value[index + 2])
                && char.IsDigit(value[index + 3])
            )
            {
                var octal = value.Substring(index + 1, 3);
                if (Convert.ToInt32(octal, 8) is var ascii)
                {
                    builder.Append((char)ascii);
                    index += 4;
                    continue;
                }
            }

            builder.Append(value[index]);
            index++;
        }

        return builder.ToString();
    }
}
