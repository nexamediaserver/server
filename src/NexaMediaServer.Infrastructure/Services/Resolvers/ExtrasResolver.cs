// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using ExtraCategory = NexaMediaServer.Infrastructure.Services.Resolvers.ExtrasResolverExtraCategory;
using ExtraClassification = NexaMediaServer.Infrastructure.Services.Resolvers.ExtrasResolverClassification;
using IODirectory = System.IO.Directory;
using IOFile = System.IO.File;
using OwnerResolutionStatus = NexaMediaServer.Infrastructure.Services.Resolvers.ExtrasResolverOwnerResolutionStatus;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves local trailer/clip extras stored alongside a movie folder.
/// </summary>
public sealed partial class ExtrasResolver : ItemResolverBase<MetadataBaseItem>
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

    private static readonly Dictionary<string, ExtraCategory> InlineSuffixToCategory = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["behindthescenes"] = ExtraCategory.BehindTheScenes,
        ["deleted"] = ExtraCategory.DeletedScene,
        ["featurette"] = ExtraCategory.Featurette,
        ["interview"] = ExtraCategory.Interview,
        ["scene"] = ExtraCategory.Scene,
        ["short"] = ExtraCategory.Short,
        ["trailer"] = ExtraCategory.Trailer,
        ["other"] = ExtraCategory.Other,
    };

    private static readonly Dictionary<string, ExtraCategory> FolderCategoryMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["behindthescenes"] = ExtraCategory.BehindTheScenes,
        ["deletedscenes"] = ExtraCategory.DeletedScene,
        ["featurettes"] = ExtraCategory.Featurette,
        ["interviews"] = ExtraCategory.Interview,
        ["scenes"] = ExtraCategory.Scene,
        ["shorts"] = ExtraCategory.Short,
        ["trailers"] = ExtraCategory.Trailer,
        ["other"] = ExtraCategory.Other,
    };

    private static readonly Regex PartRegex = new(
        @"(?:(?:cd|disc|part|pt)[ _.-]?(\d{1,2}))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex SampleRegex = new(
        @"\bsample\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private readonly ILogger<ExtrasResolver> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtrasResolver"/> class.
    /// </summary>
    /// <param name="logger">Typed logger for diagnostics.</param>
    public ExtrasResolver(ILogger<ExtrasResolver> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public override int Priority => 50;

    /// <inheritdoc />
    public override MetadataBaseItem? Resolve(ItemResolveArgs args)
    {
        if (args.LibraryType != LibraryType.Movies)
        {
            return null;
        }

        if (args.File.IsDirectory)
        {
            return null;
        }

        if (!IsVideoFile(args.File.Extension))
        {
            return null;
        }

        if (!TryClassifyExtra(args.File.Path, args.File.Name, out var classification))
        {
            return null;
        }

        var ownerStatus = TryResolveOwnerMovie(classification.MovieFolder, out var ownerPath);

        if (ownerStatus != OwnerResolutionStatus.Success)
        {
            switch (ownerStatus)
            {
                case OwnerResolutionStatus.MissingMovieFolder:
                    LogMissingMovieFolder(this.logger, args.File.Path, classification.MovieFolder);
                    break;
                case OwnerResolutionStatus.NoEligibleFiles:
                    LogNoOwnerCandidate(this.logger, args.File.Path, classification.MovieFolder);
                    break;
                case OwnerResolutionStatus.AmbiguousCandidates:
                    LogAmbiguousOwnerCandidates(
                        this.logger,
                        args.File.Path,
                        classification.MovieFolder
                    );
                    break;
            }

            return null;
        }

        return CreateMetadata(args, classification, ownerPath);
    }

    private static MetadataBaseItem CreateMetadata(
        ItemResolveArgs args,
        ExtraClassification classification,
        string ownerPath
    )
    {
        var metadata = CreateMetadataForCategory(classification.Category);
        metadata.LibrarySectionId = args.LibrarySectionId;
        metadata.Title = classification.Title;
        metadata.SortTitle = classification.Title;

        var parts = BuildMediaParts(args.File.Path);
        var mediaItem = new MediaItem
        {
            SectionLocationId = args.SectionLocationId,
            FileFormat = GetFileExtension(args.File.Path),
            Parts = parts,
        };
        foreach (var part in parts)
        {
            part.MediaItem = mediaItem;
        }

        metadata.MediaItems = new List<MediaItem> { mediaItem };

        metadata.PendingRelations.Add(
            new PendingMetadataRelation(RelationType.ClipSupplementsMetadata, ownerPath)
        );

        return metadata;
    }

    private static MetadataBaseItem CreateMetadataForCategory(ExtraCategory category) =>
        category switch
        {
            ExtraCategory.Trailer => new Trailer(),
            ExtraCategory.BehindTheScenes => new BehindTheScenes(),
            ExtraCategory.DeletedScene => new DeletedScene(),
            ExtraCategory.Featurette => new Featurette(),
            ExtraCategory.Interview => new Interview(),
            ExtraCategory.Scene => new Scene(),
            ExtraCategory.Short => new ShortForm(),
            _ => new ExtraOther(),
        };

    private static List<MediaPart> BuildMediaParts(string filePath)
    {
        var parts = new List<MediaPart>();
        try
        {
            var info = new FileInfo(filePath);
            parts.Add(new MediaPart { File = filePath, Size = info.Exists ? info.Length : null });
        }
        catch
        {
            parts.Add(new MediaPart { File = filePath });
        }

        return parts;
    }

    private static string GetFileExtension(string path)
    {
        var ext = Path.GetExtension(path)?.TrimStart('.');
        return string.IsNullOrWhiteSpace(ext) ? string.Empty : ext.ToLowerInvariant();
    }

    private static bool TryClassifyExtra(
        string filePath,
        string fileName,
        out ExtraClassification classification
    )
    {
        classification = default;
        var parent = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(parent))
        {
            return false;
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
        if (
            TryGetInlineExtraCategory(
                fileNameWithoutExtension,
                out var inlineCategory,
                out var title
            )
        )
        {
            classification = new ExtraClassification(inlineCategory, parent, title);
            return true;
        }

        var folderName = Path.GetFileName(parent) ?? string.Empty;
        if (!TryGetFolderCategory(folderName, out var folderCategory))
        {
            return false;
        }

        var movieFolder = Path.GetDirectoryName(parent);
        if (string.IsNullOrWhiteSpace(movieFolder))
        {
            return false;
        }

        var clipTitle = CleanTitle(Path.GetFileNameWithoutExtension(fileName) ?? string.Empty);
        classification = new ExtraClassification(folderCategory, movieFolder, clipTitle);
        return true;
    }

    private static OwnerResolutionStatus TryResolveOwnerMovie(
        string movieFolder,
        out string ownerPath
    )
    {
        ownerPath = string.Empty;
        if (!IODirectory.Exists(movieFolder))
        {
            return OwnerResolutionStatus.MissingMovieFolder;
        }

        var discCandidate = TryGetDiscCandidate(movieFolder);
        if (!string.IsNullOrWhiteSpace(discCandidate))
        {
            ownerPath = discCandidate!;
            return OwnerResolutionStatus.Success;
        }

        var files = IODirectory
            .EnumerateFiles(movieFolder)
            .Where(file => IsVideoFile(Path.GetExtension(file)))
            .Where(file =>
                !IsInlineExtraFile(Path.GetFileNameWithoutExtension(file) ?? string.Empty)
            )
            .Where(file => !IsSampleFile(file))
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();

        if (files.Count == 0)
        {
            return OwnerResolutionStatus.NoEligibleFiles;
        }

        if (files.Count == 1)
        {
            ownerPath = files[0];
            return OwnerResolutionStatus.Success;
        }

        if (TryCollapseStack(files, out var collapsed))
        {
            ownerPath = collapsed;
            return OwnerResolutionStatus.Success;
        }

        return OwnerResolutionStatus.AmbiguousCandidates;
    }

    private static string? TryGetDiscCandidate(string movieFolder)
    {
        var dvdPath = Path.Combine(movieFolder, "VIDEO_TS");
        if (IODirectory.Exists(dvdPath) && IOFile.Exists(Path.Combine(dvdPath, "VIDEO_TS.IFO")))
        {
            return NormalizePath(dvdPath);
        }

        var bdmvPath = Path.Combine(movieFolder, "BDMV");
        if (IODirectory.Exists(bdmvPath))
        {
            var hasStreamFiles = IODirectory
                .EnumerateFiles(bdmvPath, "*.m2ts", SearchOption.AllDirectories)
                .Any();
            if (hasStreamFiles)
            {
                return NormalizePath(bdmvPath);
            }
        }

        return null;
    }

    private static bool TryCollapseStack(IReadOnlyList<string> files, out string collapsedPath)
    {
        collapsedPath = string.Empty;
        var bases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            var match = PartRegex.Match(name);
            if (!match.Success)
            {
                return false;
            }

            var baseName = PartRegex.Replace(name, string.Empty).Trim();
            bases.Add(baseName);
        }

        if (bases.Count == 1)
        {
            collapsedPath = files.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).First();
            return true;
        }

        return false;
    }

    private static bool TryGetInlineExtraCategory(
        string fileNameWithoutExtension,
        out ExtraCategory category,
        out string title
    )
    {
        category = default;
        title = string.Empty;
        var separatorIndex = fileNameWithoutExtension.LastIndexOf('-');
        if (separatorIndex <= 0)
        {
            return false;
        }

        var suffix = fileNameWithoutExtension[(separatorIndex + 1)..];
        var normalizedSuffix = NormalizeToken(suffix);
        if (string.IsNullOrEmpty(normalizedSuffix))
        {
            return false;
        }

        if (!InlineSuffixToCategory.TryGetValue(normalizedSuffix, out category))
        {
            return false;
        }

        var rawTitle = fileNameWithoutExtension[..separatorIndex];
        title = CleanTitle(rawTitle);
        return !string.IsNullOrWhiteSpace(title);
    }

    private static bool TryGetFolderCategory(string folderName, out ExtraCategory category)
    {
        var key = NormalizeToken(folderName);
        if (string.IsNullOrEmpty(key))
        {
            category = default;
            return false;
        }

        return FolderCategoryMap.TryGetValue(key, out category);
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        return cleaned.ToLowerInvariant();
    }

    private static string CleanTitle(string rawTitle)
    {
        var title = rawTitle.Replace('_', ' ').Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title.Trim());
    }

    private static bool IsVideoFile(string? extension) =>
        !string.IsNullOrWhiteSpace(extension) && VideoExtensions.Contains(extension);

    private static bool IsSampleFile(string filePath) => SampleRegex.IsMatch(filePath);

    private static bool IsInlineExtraFile(string fileNameWithoutExtension) =>
        TryGetInlineExtraCategory(fileNameWithoutExtension, out _, out _);

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Skipping extra '{ExtraPath}' because movie folder '{MovieFolder}' is missing."
    )]
    private static partial void LogMissingMovieFolder(
        ILogger logger,
        string extraPath,
        string movieFolder
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Skipping extra '{ExtraPath}' because movie folder '{MovieFolder}' had no eligible video candidates."
    )]
    private static partial void LogNoOwnerCandidate(
        ILogger logger,
        string extraPath,
        string movieFolder
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Skipping extra '{ExtraPath}' because movie folder '{MovieFolder}' produced ambiguous owners."
    )]
    private static partial void LogAmbiguousOwnerCandidates(
        ILogger logger,
        string extraPath,
        string movieFolder
    );
}
