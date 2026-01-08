// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Discovers local artwork files adjacent to media files using Kodi and Plex naming conventions.
/// Local artwork takes absolute precedence over NFO/embedded/agent artwork.
/// </summary>
/// <remarks>
/// Supported patterns (case-insensitive, extensions: .jpg, .jpeg, .png, .webp, .gif, .tbn):
/// <list type="bullet">
///   <item><description>Posters: poster.*, cover.*, {moviename}-poster.*, {moviename}.* (Plex fallback)</description></item>
///   <item><description>Backdrops: fanart.*, backdrop.*, background.*, {moviename}-fanart.*</description></item>
///   <item><description>Logos: logo.*, clearlogo.*</description></item>
/// </list>
/// See <see href="https://kodi.wiki/view/Movie_artwork#Local_Artwork"/> and
/// <see href="https://support.plex.tv/articles/200220677-local-media-assets-movies/"/>.
/// </remarks>
public sealed partial class LocalArtworkParser : ISidecarParser
{
    private static readonly string[] SupportedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif",
        ".tbn",
    ];

    /// <summary>
    /// Poster filenames in priority order (canonical names first).
    /// </summary>
    private static readonly string[] PosterNames = ["poster", "cover", "folder", "movie"];

    /// <summary>
    /// Backdrop/fanart filenames in priority order.
    /// </summary>
    private static readonly string[] BackdropNames = ["fanart", "backdrop", "background", "art"];

    /// <summary>
    /// Logo filenames in priority order.
    /// </summary>
    private static readonly string[] LogoNames = ["logo", "clearlogo"];

    /// <inheritdoc />
    public string Name => "local-artwork";

    /// <inheritdoc />
    public string DisplayName => "Local Artwork";

    /// <inheritdoc />
    public string Description =>
        "Discovers local artwork files (poster, fanart, logo) adjacent to media files";

    /// <inheritdoc />
    public int Order => (int)MetadataAgentPriority.Sidecar;

    /// <inheritdoc />
    public IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; } = [LibraryType.Movies];

    /// <inheritdoc />
    public bool CanParse(FileSystemMetadata sidecarFile)
    {
        // We claim to parse image files so we can discover all artwork in a single pass.
        // The pipeline will call us for each image sibling.
        if (!sidecarFile.Exists || sidecarFile.IsDirectory)
        {
            return false;
        }

        var canParse = IsSupportedImageExtension(sidecarFile.Extension);

        return canParse;
    }

    /// <inheritdoc />
    public Task<SidecarParseResult?> ParseAsync(
        SidecarParseRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.LibraryType != LibraryType.Movies)
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var mediaFile = request.MediaFile;
        var mediaBaseName = Path.GetFileNameWithoutExtension(mediaFile.Path);

        // Use pre-enumerated siblings when available, otherwise scan directory
        List<string> imageFiles;
        if (request.Siblings is { Count: > 0 })
        {
            imageFiles = GetImageFilesFromSiblings(request.Siblings);
        }
        else
        {
            var directoryPath = Path.GetDirectoryName(mediaFile.Path);
            imageFiles = string.IsNullOrWhiteSpace(directoryPath)
                ? []
                : EnumerateImageFiles(directoryPath);
        }

        if (imageFiles.Count == 0)
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var poster = SelectBestArtwork(imageFiles, mediaBaseName, PosterNames, isPoster: true);
        var backdrop = SelectBestArtwork(imageFiles, mediaBaseName, BackdropNames, isPoster: false);
        var logo = SelectBestArtwork(imageFiles, mediaBaseName, LogoNames, isPoster: false);

        if (poster is null && backdrop is null && logo is null)
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var metadata = new Movie
        {
            ThumbUri = poster,
            ArtUri = backdrop,
            LogoUri = logo,
        };

        var result = new SidecarParseResult(metadata, Hints: null, Source: this.Name);

        return Task.FromResult<SidecarParseResult?>(result);
    }

    /// <summary>
    /// Selects the best artwork file matching the given naming patterns.
    /// Priority: canonical names (poster.jpg) > movie-prefixed (MovieName-poster.jpg) > fallbacks.
    /// </summary>
    private static string? SelectBestArtwork(
        IReadOnlyList<string> imageFiles,
        string mediaBaseName,
        string[] artworkNames,
        bool isPoster
    )
    {
        // Priority 1: Exact canonical names (e.g., "poster.jpg", "fanart.png")
        foreach (var name in artworkNames)
        {
            var match = FindMatchingFile(imageFiles, name);
            if (match is not null)
            {
                return match;
            }
        }

        // Priority 2: Movie-prefixed names (e.g., "MovieName-poster.jpg", "MovieName-fanart.jpg")
        foreach (var name in artworkNames)
        {
            var prefixedName = $"{mediaBaseName}-{name}";
            var match = FindMatchingFile(imageFiles, prefixedName);
            if (match is not null)
            {
                return match;
            }
        }

        // Priority 3 (Plex poster fallback): MovieName.jpg as poster
        if (isPoster)
        {
            var plexMatch = FindMatchingFile(imageFiles, mediaBaseName);
            if (plexMatch is not null)
            {
                return plexMatch;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a file matching the given base name (without extension) from the list of image files.
    /// </summary>
    private static string? FindMatchingFile(IReadOnlyList<string> imageFiles, string baseName)
    {
        foreach (var filePath in imageFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (string.Equals(fileName, baseName, StringComparison.OrdinalIgnoreCase))
            {
                return filePath;
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates all image files in the directory with supported extensions.
    /// </summary>
    private static List<string> EnumerateImageFiles(string directoryPath)
    {
        try
        {
            return Directory
                .EnumerateFiles(directoryPath)
                .Where(f => IsSupportedImageExtension(Path.GetExtension(f)))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Extracts image file paths from pre-enumerated siblings.
    /// </summary>
    private static List<string> GetImageFilesFromSiblings(
        IReadOnlyList<FileSystemMetadata> siblings
    )
    {
        return siblings
            .Where(s => s.Exists && !s.IsDirectory && IsSupportedImageExtension(s.Extension))
            .Select(s => s.Path)
            .ToList();
    }

    private static bool IsSupportedImageExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return SupportedImageExtensions.Any(ext =>
            string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase)
        );
    }
}
