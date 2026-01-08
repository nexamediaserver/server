// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text.RegularExpressions;

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

using IODir = System.IO.Directory;
using IOPath = System.IO.Path;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolves album release folders in Music libraries following Plex and Jellyfin conventions.
/// </summary>
/// <remarks>
/// <para>
/// This resolver handles directories that contain audio files and creates the full
/// album hierarchy: AlbumReleaseGroup → AlbumRelease → AlbumMedium → Track.
/// </para>
/// <para>
/// Supported folder structures:
/// <list type="bullet">
///   <item><description>Plex: <c>Music/ArtistName/AlbumName/TrackNumber - TrackName.ext</c>.</description></item>
///   <item><description>Jellyfin: <c>Music/Artist/Album/Song.flac</c> or <c>Music/Album/Song.flac</c>.</description></item>
/// </list>
/// </para>
/// <para>
/// Multi-disc album support:
/// <list type="bullet">
///   <item><description>Disc subfolders: <c>CD1</c>, <c>Disc 1</c>, <c>Disk_2</c> (Jellyfin style).</description></item>
///   <item><description>Plex track numbering: <c>101 - Track.mp3</c> for disc 1, <c>201 - Track.mp3</c> for disc 2.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed partial class MusicAlbumResolver : ItemResolverBase<AlbumReleaseGroup>
{
    private const string DefaultDiscTitle = "Disc 1";

    /// <inheritdoc />
    public override string Name => nameof(MusicAlbumResolver);

    /// <inheritdoc />
    public override int Priority => 10; // Higher priority than track resolver for directories

    /// <inheritdoc />
    public override AlbumReleaseGroup? Resolve(ItemResolveArgs args)
    {
        // Only operate within Music libraries
        if (args.LibraryType != LibraryType.Music)
        {
            return null;
        }

        // Only handle directories
        if (!args.File.IsDirectory)
        {
            return null;
        }

        // Skip library root
        if (args.IsRoot)
        {
            return null;
        }

        // Skip if this looks like a disc folder (handled by parent album)
        if (IsDiscFolder(args.File.Name))
        {
            return null;
        }

        var children = GetChildren(args);

        // Check if this is an album folder (contains audio files or disc subfolders)
        var audioFiles = children
            .Where(c => !c.IsDirectory && MediaFileExtensions.IsAudio(c.Extension))
            .ToList();
        var hasAudioFiles = audioFiles.Count > 0;

        var discFolders = children
            .Where(c => c.IsDirectory && IsDiscFolder(c.Name))
            .OrderBy(c => GetDiscNumber(c.Name))
            .ToList();
        var hasDiscFolders = discFolders.Count > 0;

        // If no audio files and no disc folders, this might be an artist folder
        // (contains album subfolders but no direct audio) - let artist resolver handle it
        if (!hasAudioFiles && !hasDiscFolders)
        {
            return null;
        }

        return CreateAlbumHierarchy(args, audioFiles, discFolders);
    }

    /// <summary>
    /// Regex for standard track numbering: "01 - Title" or "01. Title" or "01 Title".
    /// </summary>
    [GeneratedRegex(@"^(\d{1,3})\s*[-.\s]+\s*(.+)$", RegexOptions.Compiled)]
    private static partial Regex StandardTrackRegex();

    /// <summary>
    /// Regex for Plex multi-disc numbering: "101 - Title" (disc 1, track 01).
    /// First digit is disc number, next two are track number.
    /// </summary>
    [GeneratedRegex(@"^(\d)(\d{2})\s*[-.\s]+\s*(.+)$", RegexOptions.Compiled)]
    private static partial Regex PlexMultiDiscTrackRegex();

    /// <summary>
    /// Regex pattern for detecting disc folder names.
    /// Matches: CD1, CD 1, Disc1, Disc 1, Disk1, Disk 1, etc.
    /// </summary>
    [GeneratedRegex(
        @"^(?:cd|disc|disk)[\s._-]?(\d{1,2})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    )]
    private static partial Regex DiscFolderRegex();

    private static AlbumReleaseGroup CreateAlbumHierarchy(
        ItemResolveArgs args,
        List<FileSystemMetadata> rootAudioFiles,
        List<FileSystemMetadata> discFolders
    )
    {
        var albumTitle = args.File.Name;

        // Create the AlbumReleaseGroup (top of hierarchy for browsing)
        var releaseGroup = new AlbumReleaseGroup
        {
            Title = albumTitle,
            SortTitle = albumTitle,
            LibrarySectionId = args.LibrarySectionId,
        };

        // Create the AlbumRelease (specific edition)
        var albumRelease = new AlbumRelease
        {
            Title = albumTitle,
            SortTitle = albumTitle,
            LibrarySectionId = args.LibrarySectionId,
            Parent = releaseGroup,
        };
        releaseGroup.Children.Add(albumRelease);

        // Track absolute index across all discs
        var absoluteTrackIndex = 1;

        // Build disc structure and tracks
        if (discFolders.Count > 0)
        {
            // Multi-disc album with subfolders
            foreach (var discFolder in discFolders)
            {
                var discNumber = GetDiscNumber(discFolder.Name);
                var medium = CreateMediumWithTracks(
                    args,
                    discFolder.Name,
                    discNumber,
                    discFolder.Path,
                    albumRelease,
                    ref absoluteTrackIndex
                );
                albumRelease.Children.Add(medium);
            }

            // If there are also audio files at the root level, add them as disc 0
            if (rootAudioFiles.Count > 0)
            {
                var rootAbsoluteIndex = 1;
                var rootMedium = CreateMediumWithTracksFromFiles(
                    args,
                    DefaultDiscTitle,
                    0,
                    rootAudioFiles,
                    albumRelease,
                    detectMultiDisc: false,
                    ref rootAbsoluteIndex
                );
                albumRelease.Children = new List<MetadataBaseItem> { rootMedium }
                    .Concat(albumRelease.Children)
                    .ToList();

                // Adjust absolute indices for subsequent discs
                absoluteTrackIndex = rootAbsoluteIndex;
                foreach (var child in albumRelease.Children.Skip(1).OfType<AlbumMedium>())
                {
                    foreach (var track in child.Children.OfType<Track>())
                    {
                        track.AbsoluteIndex = absoluteTrackIndex++;
                    }
                }
            }
        }
        else if (rootAudioFiles.Count > 0)
        {
            // Single folder - detect Plex multi-disc naming (101, 201, etc.)
            var discGroups = GroupTracksByDisc(rootAudioFiles);

            foreach (var (discNumber, discFiles) in discGroups.OrderBy(g => g.Key))
            {
                var discTitle = discGroups.Count > 1
                    ? $"Disc {discNumber.ToString(CultureInfo.InvariantCulture)}"
                    : DefaultDiscTitle;
                var medium = CreateMediumWithTracksFromFiles(
                    args,
                    discTitle,
                    discNumber,
                    discFiles,
                    albumRelease,
                    detectMultiDisc: discGroups.Count > 1,
                    ref absoluteTrackIndex
                );
                albumRelease.Children.Add(medium);
            }
        }

        return releaseGroup;
    }

    private static AlbumMedium CreateMediumWithTracks(
        ItemResolveArgs args,
        string discTitle,
        int discNumber,
        string discFolderPath,
        AlbumRelease parent,
        ref int absoluteTrackIndex
    )
    {
        var audioFiles = GetAudioFilesInFolder(discFolderPath);
        return CreateMediumWithTracksFromFiles(
            args,
            discTitle,
            discNumber,
            audioFiles,
            parent,
            detectMultiDisc: false,
            ref absoluteTrackIndex
        );
    }

    private static AlbumMedium CreateMediumWithTracksFromFiles(
        ItemResolveArgs args,
        string discTitle,
        int discNumber,
        List<FileSystemMetadata> audioFiles,
        AlbumRelease parent,
        bool detectMultiDisc,
        ref int absoluteTrackIndex
    )
    {
        var medium = new AlbumMedium
        {
            Title = discTitle,
            SortTitle = discTitle,
            LibrarySectionId = args.LibrarySectionId,
            Index = discNumber,
            Parent = parent,
        };

        // Parse track info from all files first to determine proper ordering
        var tracksWithInfo = audioFiles
            .Select(f => new
            {
                File = f,
                ParsedInfo = ParseTrackInfo(
                    IOPath.GetFileNameWithoutExtension(f.Name),
                    detectMultiDisc
                )
            })
            .ToList();

        // Sort by parsed track number (if available), otherwise by filename
        var orderedTracks = tracksWithInfo
            .OrderBy(t => t.ParsedInfo.TrackNumber ?? int.MaxValue)
            .ThenBy(t => t.File.Name)
            .ToList();

        // Create tracks with sequential indices
        var trackIndexWithinDisc = 1;
        foreach (var trackInfo in orderedTracks)
        {
            var track = CreateTrack(
                args,
                trackInfo.File,
                medium,
                trackInfo.ParsedInfo,
                trackIndexWithinDisc,
                absoluteTrackIndex
            );
            medium.Children.Add(track);

            trackIndexWithinDisc++;
            absoluteTrackIndex++;
        }

        return medium;
    }

    private static Track CreateTrack(
        ItemResolveArgs args,
        FileSystemMetadata audioFile,
        AlbumMedium parent,
        (string Title, int? TrackNumber, int? DiscNumber) parsedInfo,
        int trackIndexWithinDisc,
        int absoluteIndex
    )
    {
        var extension = audioFile.Extension?.TrimStart('.').ToLowerInvariant();

        var part = new MediaPart
        {
            File = audioFile.Path,
            Size = audioFile.Size,
            ModifiedAt = audioFile.LastModifiedTimeUtc,
        };

        var mediaItem = new MediaItem
        {
            SectionLocationId = args.SectionLocationId,
            FileFormat = string.IsNullOrEmpty(extension) ? null : extension,
            FileSizeBytes = audioFile.Size,
            Parts = new List<MediaPart> { part },
        };

        part.MediaItem = mediaItem;

        var track = new Track
        {
            Title = parsedInfo.Title,
            SortTitle = parsedInfo.Title,
            LibrarySectionId = args.LibrarySectionId,
            MediaItems = new List<MediaItem> { mediaItem },
            Parent = parent,
            // Use parsed track number if available, otherwise use sequential index
            Index = parsedInfo.TrackNumber ?? trackIndexWithinDisc,
            // Always set absolute index for album-wide ordering
            AbsoluteIndex = absoluteIndex,
        };

        return track;
    }

    /// <summary>
    /// Groups audio files by disc number based on Plex-style naming (101, 201, etc.).
    /// </summary>
    private static Dictionary<int, List<FileSystemMetadata>> GroupTracksByDisc(
        List<FileSystemMetadata> audioFiles
    )
    {
        var groups = new Dictionary<int, List<FileSystemMetadata>>();

        foreach (var file in audioFiles)
        {
            var fileName = IOPath.GetFileNameWithoutExtension(file.Name);
            var match = PlexMultiDiscTrackRegex().Match(fileName);

            int discNum;
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > 0)
            {
                discNum = parsed;
            }
            else
            {
                discNum = 1; // Default to disc 1
            }

            if (!groups.TryGetValue(discNum, out var list))
            {
                list = new List<FileSystemMetadata>();
                groups[discNum] = list;
            }

            list.Add(file);
        }

        return groups;
    }

    /// <summary>
    /// Parses track information from a filename.
    /// </summary>
    private static (string Title, int? TrackNumber, int? DiscNumber) ParseTrackInfo(
        string fileName,
        bool detectMultiDisc
    )
    {
        if (detectMultiDisc)
        {
            // Try Plex multi-disc format first (e.g., "201 - Track Name" for disc 2, track 1)
            var plexMatch = PlexMultiDiscTrackRegex().Match(fileName);
            if (plexMatch.Success)
            {
                var discNum = int.Parse(plexMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var trackNum = int.Parse(plexMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                var trackTitle = plexMatch.Groups[3].Value.Trim();
                return (trackTitle, trackNum, discNum);
            }
        }

        // Try standard format (e.g., "01 - Track Name" or "01. Track Name")
        var standardMatch = StandardTrackRegex().Match(fileName);
        if (standardMatch.Success)
        {
            var trackNum = int.Parse(standardMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var trackTitle = standardMatch.Groups[2].Value.Trim();
            return (trackTitle, trackNum, null);
        }

        // No recognized format - use filename as-is
        return (fileName, null, null);
    }

    private static bool IsDiscFolder(string name) => DiscFolderRegex().IsMatch(name);

    private static int GetDiscNumber(string name)
    {
        var match = DiscFolderRegex().Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var num))
        {
            return num;
        }

        return 1;
    }

    private static List<FileSystemMetadata> GetAudioFilesInFolder(string folderPath)
    {
        try
        {
            return IODir
                .EnumerateFiles(folderPath)
                .Where(f => MediaFileExtensions.IsAudio(IOPath.GetExtension(f)))
                .Select(FileSystemMetadata.FromPath)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<FileSystemMetadata> GetChildren(ItemResolveArgs args)
    {
        if (args.FileSystemChildren is not null)
        {
            return args.FileSystemChildren;
        }

        try
        {
            return IODir
                .EnumerateFileSystemEntries(args.File.Path)
                .Select(FileSystemMetadata.FromPath)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
