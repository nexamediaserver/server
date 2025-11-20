// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;
using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;
using TagLib;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Extracts embedded metadata from audio files using TagLib#.
/// </summary>
/// <remarks>
/// <para>
/// Supports reading ID3v1/v2 (MP3), Vorbis comments (OGG, FLAC), MP4 atoms (M4A, AAC),
/// APE tags, and other formats supported by TagLib#.
/// </para>
/// <para>
/// Extracts MusicBrainz identifiers when present:
/// <list type="bullet">
///   <item><description>MUSICBRAINZ_TRACKID - Track ID</description></item>
///   <item><description>MUSICBRAINZ_RELEASEID - Release ID</description></item>
///   <item><description>MUSICBRAINZ_RELEASETRACKID - Release Track ID</description></item>
///   <item><description>MUSICBRAINZ_RELEASEGROUPID - Release Group ID</description></item>
///   <item><description>MUSICBRAINZ_ARTISTID - Artist ID</description></item>
///   <item><description>MUSICBRAINZ_ALBUMARTISTID - Album Artist ID</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed partial class TagLibMetadataExtractor(ILogger<TagLibMetadataExtractor> logger)
    : IEmbeddedMetadataExtractor
{
    /// <inheritdoc />
    public string Name => nameof(TagLibMetadataExtractor);

    /// <inheritdoc />
    public string DisplayName => "Audio Tag Reader";

    /// <inheritdoc />
    public string Description =>
        "Extracts metadata from audio file tags (ID3, Vorbis, MP4, APE, etc.)";

    /// <inheritdoc />
    public int Order => 100; // Run early for audio files

    /// <inheritdoc />
    public IReadOnlyCollection<LibraryType> SupportedLibraryTypes =>
        new[] { LibraryType.Music, LibraryType.Audiobooks, LibraryType.Podcasts };

    /// <inheritdoc />
    public bool CanExtract(FileSystemMetadata mediaFile) =>
        !mediaFile.IsDirectory && MediaFileExtensions.IsAudio(mediaFile.Extension);

    /// <inheritdoc />
    public Task<EmbeddedMetadataResult?> ExtractAsync(
        EmbeddedMetadataRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var file = TagLib.File.Create(request.MediaFile.Path);

            var tag = file.Tag;
            if (tag is null)
            {
                return Task.FromResult<EmbeddedMetadataResult?>(null);
            }

            var metadata = CreateMetadataFromTag(tag, file.Properties);
            var hints = ExtractHints(tag);

            return Task.FromResult<EmbeddedMetadataResult?>(
                new EmbeddedMetadataResult(metadata, hints)
            );
        }
        catch (CorruptFileException ex)
        {
            this.LogCorruptFile(request.MediaFile.Path, ex.Message);
            return Task.FromResult<EmbeddedMetadataResult?>(null);
        }
        catch (UnsupportedFormatException ex)
        {
            this.LogUnsupportedFormat(request.MediaFile.Path, ex.Message);
            return Task.FromResult<EmbeddedMetadataResult?>(null);
        }
        catch (Exception ex)
        {
            this.LogExtractionError(request.MediaFile.Path, ex.Message);
            return Task.FromResult<EmbeddedMetadataResult?>(null);
        }
    }

    private static Track CreateMetadataFromTag(Tag tag, Properties? properties)
    {
        var track = new Track
        {
            Title = string.IsNullOrWhiteSpace(tag.Title) ? string.Empty : tag.Title.Trim(),
            Year = tag.Year > 0 ? (int)tag.Year : null,
            Index = tag.Track > 0 ? (int)tag.Track : null,
        };

        // Duration from properties
        if (properties?.Duration.TotalSeconds > 0)
        {
            track.Duration = (int)properties.Duration.TotalSeconds;
        }

        return track;
    }

    private static Dictionary<string, object>? ExtractHints(Tag tag)
    {
        var hints = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Artist information
        AddIfNotEmpty(hints, "artist", GetFirstNonEmpty(tag.Performers));
        AddIfNotEmpty(hints, "album_artist", GetFirstNonEmpty(tag.AlbumArtists));
        AddIfNotEmpty(hints, "album", tag.Album);
        AddIfNotEmpty(
            hints,
            "disc",
            tag.Disc > 0
                ? tag.Disc.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null
        );
        AddIfNotEmpty(
            hints,
            "disc_count",
            tag.DiscCount > 0
                ? tag.DiscCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null
        );
        AddIfNotEmpty(
            hints,
            "track_count",
            tag.TrackCount > 0
                ? tag.TrackCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null
        );

        // Genres
        if (tag.Genres?.Length > 0)
        {
            var validGenres = tag
                .Genres.Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .ToList();
            if (validGenres.Count > 0)
            {
                hints["genres"] = validGenres;
            }
        }

        // All performers/artists for potential Person/Group creation
        if (tag.Performers?.Length > 0)
        {
            var performers = tag
                .Performers.Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();
            if (performers.Count > 0)
            {
                hints["performers"] = performers;
            }
        }

        if (tag.AlbumArtists?.Length > 0)
        {
            var albumArtists = tag
                .AlbumArtists.Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .ToList();
            if (albumArtists.Count > 0)
            {
                hints["album_artists"] = albumArtists;
            }
        }

        // External IDs (MusicBrainz, etc.)
        var externalIds = ExtractExternalIds(tag);
        if (externalIds.Count > 0)
        {
            hints["external_ids"] = externalIds;
        }

        return hints.Count > 0 ? hints : null;
    }

    private static Dictionary<string, string> ExtractExternalIds(Tag tag)
    {
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // MusicBrainz IDs are available directly on the Tag object
        AddIfNotEmpty(ids, "musicbrainz_track", tag.MusicBrainzTrackId);
        AddIfNotEmpty(ids, "musicbrainz_release", tag.MusicBrainzReleaseId);
        AddIfNotEmpty(ids, "musicbrainz_release_group", tag.MusicBrainzReleaseGroupId);
        AddIfNotEmpty(ids, "musicbrainz_artist", tag.MusicBrainzArtistId);
        AddIfNotEmpty(ids, "musicbrainz_release_artist", tag.MusicBrainzReleaseArtistId);

        // Amazon ASIN if present
        AddIfNotEmpty(ids, "amazon", tag.AmazonId);

        // MusicIP PUID if present
        AddIfNotEmpty(ids, "musicip_puid", tag.MusicIpId);

        return ids;
    }

    private static string? GetFirstNonEmpty(string[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return null;
        }

        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .FirstOrDefault();
    }

    private static void AddIfNotEmpty(Dictionary<string, object> dict, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dict[key] = value.Trim();
        }
    }

    private static void AddIfNotEmpty(Dictionary<string, string> dict, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dict[key] = value.Trim();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Corrupt audio file at {Path}: {Error}")]
    private partial void LogCorruptFile(string path, string error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unsupported audio format at {Path}: {Error}")]
    private partial void LogUnsupportedFormat(string path, string error);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error extracting metadata from {Path}: {Error}"
    )]
    private partial void LogExtractionError(string path, string error);
}
