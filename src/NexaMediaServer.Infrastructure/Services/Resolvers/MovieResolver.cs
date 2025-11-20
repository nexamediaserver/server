// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using IODir = System.IO.Directory;
using IOPath = System.IO.Path;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves movie items in Movies libraries. Ignores extras/trailers and non-movie libraries.
/// </summary>
public class MovieResolver : ItemResolverBase
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".264",
        ".265",
        ".3g2",
        ".3gp",
        ".amv",
        ".asf",
        ".avi",
        ".divx",
        ".dvr-ms",
        ".f4v",
        ".flv",
        ".gxf",
        ".h264",
        ".h265",
        ".hevc",
        ".img",
        ".ismv",
        ".iso",
        ".ivf",
        ".m1v",
        ".m2t",
        ".m2ts",
        ".m2v",
        ".m4v",
        ".mjpg",
        ".mjpeg",
        ".mk3d",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpg",
        ".mpeg",
        ".mts",
        ".mxf",
        ".nut",
        ".nuv",
        ".ogg",
        ".ogm",
        ".ogv",
        ".ogx",
        ".ps",
        ".rec",
        ".ts",
        ".rm",
        ".rmvb",
        ".vdr",
        ".vro",
        ".vob",
        ".webm",
        ".wmv",
        ".wtv",
        ".y4m",
    };

    private static readonly HashSet<string> ExtrasFolderNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // Generic
        "extras",
        "extra",
        "bonus",
        "bonus features",
        "bonus-feature",
        "bonus-featurettes",
        // Common Plex categories
        "featurette",
        "featurettes",
        "behind the scenes",
        "behind-the-scenes",
        "deleted scene",
        "deleted scenes",
        "deleted-scenes",
        "interview",
        "interviews",
        "scene",
        "scenes",
        "short",
        "shorts",
        "trailer",
        "trailers",
        "teaser",
        "teasers",
        "promo",
        "promos",
        "preview",
        "previews",
        "bloopers",
        // Less common catch-alls
        "other",
    };

    private static readonly Regex SampleRegex = new(
        @"\bsample\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private static readonly Regex PartRegex = new(
        @"(?:(?:cd|disc|part|pt)[ _.-]?(\d{1,2}))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Extras filename patterns based on Plex's supported local extras naming
    // Matches tokens like: " - trailer", "_featurette", "(behind the scenes)", etc.
    private static readonly Regex ExtrasNameRegex = new(
        @"(?ix)
        (?:^|[^\p{L}\p{N}])                 # start or a non-letter/digit delimiter
        (
          behind[\s._\-]?the[\s._\-]?scenes
        | deleted[\s._\-]?scene(?:s)?
        | featurette(?:s)?
        | interview(?:s)?
        | blooper(?:s)?
        | scene(?:s)?
        | short(?:s)?
        | teaser(?:s)?
        | trailer(?:s)?
        | promo(?:s)?
        | preview(?:s)?
        | bonus(?:[\s._\-](?:feature(?:tte)?s?))?  # bonus, bonus-feature(s), bonus-featurette(s)
        | extra(?:s)?                              # extra / extras
        )
        (?:$|[^\p{L}\p{N}])                 # end or a non-letter/digit delimiter
        ",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <inheritdoc />
    public override MetadataItem? Resolve(ItemResolveArgs args)
    {
        // Only operate within Movies libraries
        if (args.LibraryType != LibraryType.Movies)
        {
            return null;
        }

        if (args.File.IsDirectory)
        {
            if (IsInvalid(args))
            {
                return null;
            }

            var children = GetChildren(args);

            // Skip known extras folders from consideration
            var filtered = children
                .Where(c => !(c.IsDirectory && ExtrasFolderNames.Contains(c.Name)))
                .ToList();

            // Detect optical disc structures (VIDEO_TS / BDMV in subfolder)
            foreach (var child in filtered)
            {
                if (child.IsDirectory)
                {
                    if (IsDvdDirectory(child))
                    {
                        return CreateDiscMovie(args, args.File.Name, child.Path, isBluRay: false);
                    }

                    if (IsBluRayDirectory(child))
                    {
                        return CreateDiscMovie(args, args.File.Name, child.Path, isBluRay: true);
                    }
                }
                else if (IsDvdFile(child))
                {
                    return CreateDiscMovie(args, args.File.Name, args.File.Path, isBluRay: false);
                }
            }

            // Otherwise, resolve by primary video files in the directory
            var videoFiles = filtered
                .Where(f => !f.IsDirectory && IsVideoFile(f) && !IsIgnoredName(f.Name))
                .ToList();

            if (videoFiles.Count == 0)
            {
                return null;
            }

            if (TryBuildStackedMovie(videoFiles, out var stackedParts))
            {
                return CreateFileMovie(args, args.File.Name, stackedParts);
            }

            // Fall back to single largest file as main movie
            var mainFile = videoFiles
                .Select(f => (Meta: f, Size: GetFileSizeSafe(f.Path)))
                .OrderByDescending(t => t.Size)
                .First()
                .Meta;

            return CreateFileMovie(args, args.File.Name, new[] { mainFile.Path });
        }
        else
        {
            // Single file case
            if (IsIgnoredName(args.File.Name))
            {
                return null;
            }

            if (IsDvdFile(args.File))
            {
                var parentDir = IOPath.GetDirectoryName(args.File.Path);
                var title = parentDir is null
                    ? IOPath.GetFileNameWithoutExtension(args.File.Name)
                    : IOPath.GetFileName(parentDir);
                return CreateDiscMovie(args, title, parentDir ?? args.File.Path, isBluRay: false);
            }

            if (IsVideoFile(args.File))
            {
                var title = IOPath.GetFileNameWithoutExtension(args.File.Name);
                return CreateFileMovie(args, title, new[] { args.File.Path });
            }
        }

        return null;
    }

    private static bool IsInvalid(ItemResolveArgs args)
    {
        // If it's the root of the library section, skip
        if (args.IsRoot)
        {
            return true;
        }

        return false;
    }

    private static bool IsVideoFile(FileSystemMetadata f) => VideoExtensions.Contains(f.Extension);

    private static bool IsIgnoredName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (
            lower.EndsWith(".nfo", StringComparison.Ordinal)
            || lower.EndsWith(".srt", StringComparison.Ordinal)
        )
        {
            return true;
        }

        // Known samples
        if (SampleRegex.IsMatch(name))
        {
            return true;
        }

        // Extras-like filenames
        if (ExtrasNameRegex.IsMatch(name))
        {
            return true;
        }

        return false;
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

    private static bool IsDvdDirectory(FileSystemMetadata dir)
    {
        if (
            !dir.IsDirectory
            || !string.Equals(dir.Name, "VIDEO_TS", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        try
        {
            return IODir
                .EnumerateFileSystemEntries(dir.Path)
                .Any(entry =>
                    string.Equals(
                        IOPath.GetExtension(entry),
                        ".VOB",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }
        catch
        {
            // ignore io errors
            return false;
        }
    }

    private static bool IsBluRayDirectory(FileSystemMetadata dir)
    {
        if (
            !dir.IsDirectory || !string.Equals(dir.Name, "BDMV", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        try
        {
            return IODir
                .EnumerateFileSystemEntries(dir.Path, "*", System.IO.SearchOption.AllDirectories)
                .Any(entry =>
                    string.Equals(
                        IOPath.GetExtension(entry),
                        ".M2TS",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }
        catch
        {
            // ignore io errors
            return false;
        }
    }

    private static bool IsDvdFile(FileSystemMetadata file)
    {
        return !file.IsDirectory
            && string.Equals(
                IOPath.GetFileName(file.Path),
                "VIDEO_TS.IFO",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static long GetFileSizeSafe(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            return fi.Exists ? fi.Length : 0L;
        }
        catch
        {
            return 0L;
        }
    }

    private static bool TryBuildStackedMovie(
        List<FileSystemMetadata> files,
        out IReadOnlyList<string> orderedParts
    )
    {
        // Detect parts by common part pattern (cd1, part1, disc1, pt1)
        var matches = new List<(FileSystemMetadata File, int Index)>();
        foreach (var f in files)
        {
            var m = PartRegex.Match(f.Name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var idx))
            {
                matches.Add((f, idx));
            }
        }

        if (matches.Count >= 2)
        {
            orderedParts = matches.OrderBy(t => t.Index).Select(t => t.File.Path).ToList();
            return true;
        }

        orderedParts = Array.Empty<string>();
        return false;
    }

    private static MetadataItem CreateDiscMovie(
        ItemResolveArgs args,
        string title,
        string path,
        bool isBluRay
    )
    {
        var mediaItem = new MediaItem
        {
            SectionLocationId = args.SectionLocationId,
            IsDisc = true,
            DiscType = isBluRay ? DiscType.BluRay : DiscType.DVD,
            FileFormat = isBluRay ? "bluray" : "dvd",
            Parts = new List<MediaPart> { new MediaPart { File = path } },
        };
        foreach (var part in mediaItem.Parts)
        {
            part.MediaItem = mediaItem;
        }

        return new MetadataItem
        {
            MetadataType = MetadataType.Movie,
            Title = title,
            SortTitle = title,
            LibrarySectionId = args.LibrarySectionId,
            MediaItems = new List<MediaItem> { mediaItem },
        };
    }

    private static MetadataItem CreateFileMovie(
        ItemResolveArgs args,
        string title,
        IReadOnlyList<string> partPaths
    )
    {
        var parts = partPaths
            .Select(p =>
            {
                try
                {
                    var fi = new FileInfo(p);
                    return new MediaPart { File = p, Size = fi.Exists ? fi.Length : null };
                }
                catch
                {
                    return new MediaPart { File = p };
                }
            })
            .ToList();
        var firstExt = IOPath.GetExtension(partPaths[0]);
        firstExt = firstExt?.TrimStart('.')?.ToLowerInvariant();
        var mediaItem = new MediaItem
        {
            SectionLocationId = args.SectionLocationId,
            FileFormat = string.IsNullOrEmpty(firstExt) ? null : firstExt,
            Parts = parts,
        };
        foreach (var part in parts)
        {
            part.MediaItem = mediaItem;
        }

        // aggregate size
        try
        {
            mediaItem.FileSizeBytes = parts.Where(p => p.Size.HasValue).Sum(p => p.Size!.Value);
        }
        catch
        {
            // ignore aggregation errors
        }

        return new MetadataItem
        {
            MetadataType = MetadataType.Movie,
            Title = title,
            SortTitle = title,
            LibrarySectionId = args.LibrarySectionId,
            MediaItems = new List<MediaItem> { mediaItem },
        };
    }
}
