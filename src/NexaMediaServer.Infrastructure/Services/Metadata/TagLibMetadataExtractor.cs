// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;

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
    /// <summary>
    /// Separator characters used for multi-value external IDs (MusicBrainz stores multiple IDs separated by these).
    /// </summary>
    private static readonly char[] MultiValueIdSeparators = [';', '/'];

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
            var hints = ExtractHints(file);

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

    private static Track CreateMetadataFromTag(TagLib.Tag tag, Properties? properties)
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

    private static Dictionary<string, object>? ExtractHints(TagLib.File file)
    {
        var tag = file.Tag;
        var hints = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // ---------------------- Basic Artist/Album Information ----------------------
        ExtractBasicMetadata(hints, tag);

        // ---------------------- Sort Names ----------------------
        ExtractSortNames(hints, tag);

        // ---------------------- Release Information ----------------------
        ExtractReleaseInfo(hints, file);

        // ---------------------- Recording Information ----------------------
        ExtractRecordingInfo(hints, file);

        // ---------------------- Credits/Relationships ----------------------
        ExtractCredits(hints, file);

        // ---------------------- Classical Music ----------------------
        ExtractClassicalMusic(hints, file);

        // ---------------------- External IDs ----------------------
        var externalIds = ExtractExternalIds(file);
        if (externalIds.Count > 0)
        {
            hints[EmbeddedMetadataHintKeys.ExternalIds] = externalIds;
        }

        // ---------------------- Artist External IDs ----------------------
        ExtractArtistExternalIds(hints, tag);

        return hints.Count > 0 ? hints : null;
    }

    private static void ExtractBasicMetadata(Dictionary<string, object> hints, TagLib.Tag tag)
    {
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Artist, GetFirstNonEmpty(tag.Performers));
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.AlbumArtist, GetFirstNonEmpty(tag.AlbumArtists));
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Album, tag.Album);

        AddIfNotEmpty(
            hints,
            EmbeddedMetadataHintKeys.Disc,
            tag.Disc > 0 ? tag.Disc.ToString(CultureInfo.InvariantCulture) : null);
        AddIfNotEmpty(
            hints,
            EmbeddedMetadataHintKeys.DiscCount,
            tag.DiscCount > 0 ? tag.DiscCount.ToString(CultureInfo.InvariantCulture) : null);
        AddIfNotEmpty(
            hints,
            EmbeddedMetadataHintKeys.TrackCount,
            tag.TrackCount > 0 ? tag.TrackCount.ToString(CultureInfo.InvariantCulture) : null);

        // Genres
        if (tag.Genres?.Length > 0)
        {
            var validGenres = tag.Genres
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .ToList();
            if (validGenres.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.Genres] = validGenres;
            }
        }

        // All performers/artists for potential Person/Group creation
        if (tag.Performers?.Length > 0)
        {
            var performers = tag.Performers
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();
            if (performers.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.Performers] = performers;
            }
        }

        if (tag.AlbumArtists?.Length > 0)
        {
            var albumArtists = tag.AlbumArtists
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .ToList();
            if (albumArtists.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.AlbumArtists] = albumArtists;
            }
        }

        // Comment
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Comment, tag.Comment);
    }

    private static void ExtractSortNames(Dictionary<string, object> hints, TagLib.Tag tag)
    {
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.TitleSort, tag.TitleSort);
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.AlbumSort, tag.AlbumSort);
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ArtistSort, GetFirstNonEmpty(tag.PerformersSort));
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.AlbumArtistSort, GetFirstNonEmpty(tag.AlbumArtistsSort));
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ComposerSort, GetFirstNonEmpty(tag.ComposersSort));
    }

    private static void ExtractReleaseInfo(Dictionary<string, object> hints, TagLib.File file)
    {
        // Barcode
        var barcode = GetCustomTag(file, "BARCODE");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Barcode, barcode);

        // Catalog number (may be multi-value)
        var catalogNumber = GetCustomTag(file, "CATALOGNUMBER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.CatalogNumber, catalogNumber);

        // Label
        var label = GetCustomTag(file, "LABEL");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Label, label);

        // Media format
        var media = GetCustomTag(file, "MEDIA");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Media, media);

        // Release type
        var releaseType = GetCustomTag(file, "RELEASETYPE");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ReleaseType, releaseType);

        // Release status
        var releaseStatus = GetCustomTag(file, "RELEASESTATUS");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ReleaseStatus, releaseStatus);

        // Release country
        var releaseCountry = GetCustomTag(file, "RELEASECOUNTRY");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ReleaseCountry, releaseCountry);

        // Script
        var script = GetCustomTag(file, "SCRIPT");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Script, script);

        // Original date/year
        var originalDate = GetCustomTag(file, "ORIGINALDATE");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.OriginalDate, originalDate);

        var originalYear = GetCustomTag(file, "ORIGINALYEAR")
                          ?? GetCustomTag(file, "TDOR") // ID3v2.4 Original Release Time
                          ?? GetCustomTag(file, "TORY"); // ID3v2.3 Original Release Year
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.OriginalYear, originalYear);

        // Disc subtitle
        var discSubtitle = GetCustomTag(file, "DISCSUBTITLE")
                          ?? GetCustomTag(file, "TSST"); // ID3v2.4 Set Subtitle
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.DiscSubtitle, discSubtitle);

        // Compilation
        var compilation = GetCustomTag(file, "COMPILATION")
                         ?? GetCustomTag(file, "TCMP"); // iTunes compilation flag
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Compilation, compilation);

        // Website
        var website = GetCustomTag(file, "WEBSITE")
                     ?? GetCustomTag(file, "WOAR"); // ID3v2 Official Artist URL
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Website, website);
    }

    private static void ExtractRecordingInfo(Dictionary<string, object> hints, TagLib.File file)
    {
        var tag = file.Tag;

        // ISRC - available directly on some tags
        var isrc = GetCustomTag(file, "ISRC")
                  ?? GetCustomTag(file, "TSRC"); // ID3v2 ISRC frame
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Isrc, isrc);

        // BPM
        if (tag.BeatsPerMinute > 0)
        {
            hints[EmbeddedMetadataHintKeys.Bpm] = tag.BeatsPerMinute.ToString(CultureInfo.InvariantCulture);
        }

        // Key
        var key = GetCustomTag(file, "KEY")
                 ?? GetCustomTag(file, "TKEY") // ID3v2 Initial Key
                 ?? GetCustomTag(file, "INITIALKEY"); // Vorbis comment
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Key, key);

        // Copyright
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Copyright, tag.Copyright);

        // Lyrics
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Lyrics, tag.Lyrics);

        // Encoded by
        var encodedBy = GetCustomTag(file, "ENCODEDBY")
                       ?? GetCustomTag(file, "TENC"); // ID3v2 Encoded By
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.EncodedBy, encodedBy);

        // Encoder settings
        var encoderSettings = GetCustomTag(file, "ENCODERSETTINGS")
                             ?? GetCustomTag(file, "TSSE"); // ID3v2 Software/Hardware Settings
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.EncoderSettings, encoderSettings);
    }

    private static void ExtractCredits(Dictionary<string, object> hints, TagLib.File file)
    {
        var tag = file.Tag;

        // Composers - available directly
        if (tag.Composers?.Length > 0)
        {
            var composers = tag.Composers
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();
            if (composers.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.Composers] = composers;
            }
        }

        // Conductor - available directly
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Conductors, tag.Conductor);

        // Lyricist
        var lyricist = GetCustomTag(file, "LYRICIST")
                      ?? GetCustomTag(file, "TEXT"); // ID3v2 Lyricist
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Lyricists, lyricist);

        // Arranger
        var arranger = GetCustomTag(file, "ARRANGER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Arrangers, arranger);

        // Producer
        var producer = GetCustomTag(file, "PRODUCER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Producers, producer);

        // Engineer
        var engineer = GetCustomTag(file, "ENGINEER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Engineers, engineer);

        // Mixer
        var mixer = GetCustomTag(file, "MIXER")
                   ?? GetCustomTag(file, "MIXARTIST");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Mixers, mixer);

        // DJ Mixer
        var djMixer = GetCustomTag(file, "DJMIXER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.DjMixers, djMixer);

        // Remixer
        var remixer = GetCustomTag(file, "REMIXER")
                     ?? GetCustomTag(file, "TPE4"); // ID3v2 Interpreted/Remixed By
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Remixers, remixer);

        // Director
        var director = GetCustomTag(file, "DIRECTOR");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Directors, director);

        // Writer (general songwriters)
        var writer = GetCustomTag(file, "WRITER");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Writers, writer);

        // Extract performer credits with roles
        ExtractPerformerCredits(hints, file);
    }

    private static void ExtractPerformerCredits(Dictionary<string, object> hints, TagLib.File file)
    {
        // Try to extract PERFORMER:<role> tags (Picard format)
        var performerCredits = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Check for ID3v2 TMCL (Musician Credits List) and TIPL (Involved People List)
        if (file.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag id3v2Tag)
        {
            // Extract from TMCL frame (musician credits)
            foreach (var frame in id3v2Tag.GetFrames<TextInformationFrame>("TMCL"))
            {
                ParseInvolvedPeopleFrame(performerCredits, frame.Text);
            }

            // Extract from TIPL frame (involved people)
            foreach (var frame in id3v2Tag.GetFrames<TextInformationFrame>("TIPL"))
            {
                ParseInvolvedPeopleFrame(performerCredits, frame.Text);
            }
        }

        // Check Vorbis comments for PERFORMER tags
        // Note: XiphComment doesn't have GetFieldNames(), so we check common performer role fields
        if (file.GetTag(TagTypes.Xiph) is XiphComment vorbisTag)
        {
            // Common performer roles to check in Vorbis comments
            var performerRoles = new[]
            {
                "vocals", "lead vocals", "backing vocals", "guitar", "bass", "drums",
                "keyboards", "piano", "synthesizer", "violin", "cello", "flute",
                "saxophone", "trumpet", "percussion", "programming",
            };

            foreach (var role in performerRoles)
            {
                var fieldName = $"PERFORMER:{role}";
                var values = vorbisTag.GetField(fieldName);
                if (values?.Length > 0)
                {
                    if (!performerCredits.TryGetValue(role, out var list))
                    {
                        list = [];
                        performerCredits[role] = list;
                    }

                    list.AddRange(values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
                }
            }
        }

        if (performerCredits.Count > 0)
        {
            hints[EmbeddedMetadataHintKeys.PerformerCredits] = performerCredits
                .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }
    }

    private static void ParseInvolvedPeopleFrame(Dictionary<string, List<string>> credits, string[]? text)
    {
        if (text is null || text.Length < 2)
        {
            return;
        }

        // Format: role1, name1, role2, name2, ...
        for (var i = 0; i < text.Length - 1; i += 2)
        {
            var role = text[i]?.Trim();
            var name = text[i + 1]?.Trim();

            if (!string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(name))
            {
                if (!credits.TryGetValue(role, out var list))
                {
                    list = [];
                    credits[role] = list;
                }

                list.Add(name);
            }
        }
    }

    private static void ExtractClassicalMusic(Dictionary<string, object> hints, TagLib.File file)
    {
        // Work title
        var work = GetCustomTag(file, "WORK")
                  ?? GetCustomTag(file, "TIT1"); // ID3v2 Content Group (sometimes used for work)
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Work, work);

        // Movement title
        var movement = GetCustomTag(file, "MOVEMENT")
                      ?? GetCustomTag(file, "MVNM"); // ID3v2.4 Movement Name
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Movement, movement);

        // Movement number
        var movementNumber = GetCustomTag(file, "MOVEMENTNUMBER")
                            ?? GetCustomTag(file, "MVIN"); // ID3v2.4 Movement Number
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.MovementNumber, movementNumber);

        // Movement total
        var movementTotal = GetCustomTag(file, "MOVEMENTTOTAL");
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.MovementTotal, movementTotal);

        // Show movement flag
        var showMovement = GetCustomTag(file, "SHOWMOVEMENT")
                          ?? GetCustomTag(file, "SHWM"); // iTunes Show Work Movement
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.ShowMovement, showMovement);

        // Language (ISO 639-3)
        var language = GetCustomTag(file, "LANGUAGE")
                      ?? GetCustomTag(file, "TLAN"); // ID3v2 Language
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.Language, language);

        // License
        var license = GetCustomTag(file, "LICENSE")
                     ?? GetCustomTag(file, "WCOP"); // ID3v2 Copyright URL
        AddIfNotEmpty(hints, EmbeddedMetadataHintKeys.License, license);
    }

    private static Dictionary<string, string> ExtractExternalIds(TagLib.File file)
    {
        var tag = file.Tag;
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // MusicBrainz IDs are available directly on the Tag object
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzTrack, tag.MusicBrainzTrackId);
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzRelease, tag.MusicBrainzReleaseId);
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzReleaseGroup, tag.MusicBrainzReleaseGroupId);
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzArtist, tag.MusicBrainzArtistId);
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzReleaseArtist, tag.MusicBrainzReleaseArtistId);

        // MusicBrainz Recording ID
        var recordingId = GetCustomTag(file, "MUSICBRAINZ_TRACKID")
                         ?? GetCustomTag(file, "MUSICBRAINZ_RECORDINGID")
                         ?? GetCustomTag(file, "UFID:http://musicbrainz.org");
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzRecording, recordingId);

        // MusicBrainz Work ID
        var workId = GetCustomTag(file, "MUSICBRAINZ_WORKID");
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzWork, workId);

        // MusicBrainz Disc ID
        var discId = GetCustomTag(file, "MUSICBRAINZ_DISCID");
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzDiscId, discId);

        // MusicBrainz Original Album ID (for merged releases)
        var originalAlbumId = GetCustomTag(file, "MUSICBRAINZ_ORIGINALALBUMID");
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzOriginalAlbumId, originalAlbumId);

        // MusicBrainz Original Artist ID (for merged recordings)
        var originalArtistId = GetCustomTag(file, "MUSICBRAINZ_ORIGINALARTISTID");
        AddIfNotEmpty(ids, ExternalIdProviders.MusicBrainzOriginalArtistId, originalArtistId);

        // AcoustID
        var acoustId = GetCustomTag(file, "ACOUSTID_ID");
        AddIfNotEmpty(ids, ExternalIdProviders.AcoustId, acoustId);

        // Amazon ASIN
        AddIfNotEmpty(ids, ExternalIdProviders.Amazon, tag.AmazonId);

        // MusicIP PUID
        AddIfNotEmpty(ids, ExternalIdProviders.MusicIpPuid, tag.MusicIpId);

        // ISRC (also as external identifier)
        var isrc = GetCustomTag(file, "ISRC") ?? GetCustomTag(file, "TSRC");
        AddIfNotEmpty(ids, ExternalIdProviders.Isrc, isrc);

        // Barcode (also as external identifier)
        var barcode = GetCustomTag(file, "BARCODE");
        AddIfNotEmpty(ids, ExternalIdProviders.Barcode, barcode);

        // Discogs IDs
        var discogsRelease = GetCustomTag(file, "DISCOGS_RELEASE_ID");
        AddIfNotEmpty(ids, ExternalIdProviders.DiscogsRelease, discogsRelease);

        var discogsMaster = GetCustomTag(file, "DISCOGS_MASTER_RELEASE_ID");
        AddIfNotEmpty(ids, ExternalIdProviders.DiscogsMaster, discogsMaster);

        var discogsArtist = GetCustomTag(file, "DISCOGS_ARTIST_ID");
        AddIfNotEmpty(ids, ExternalIdProviders.DiscogsArtist, discogsArtist);

        return ids;
    }

    private static void ExtractArtistExternalIds(Dictionary<string, object> hints, TagLib.Tag tag)
    {
        // Multi-value artist IDs (semicolon-separated in MusicBrainz)
        if (!string.IsNullOrWhiteSpace(tag.MusicBrainzArtistId))
        {
            var artistIds = SplitMultiValueId(tag.MusicBrainzArtistId);
            if (artistIds.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.ArtistExternalIds] = new Dictionary<string, object>
                {
                    [ExternalIdProviders.MusicBrainzArtist] = artistIds,
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(tag.MusicBrainzReleaseArtistId))
        {
            var albumArtistIds = SplitMultiValueId(tag.MusicBrainzReleaseArtistId);
            if (albumArtistIds.Count > 0)
            {
                hints[EmbeddedMetadataHintKeys.AlbumArtistExternalIds] = new Dictionary<string, object>
                {
                    [ExternalIdProviders.MusicBrainzReleaseArtist] = albumArtistIds,
                };
            }
        }
    }

    private static List<string> SplitMultiValueId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        // MusicBrainz stores multiple IDs as semicolon or slash separated
        return value
            .Split(MultiValueIdSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
    }

    /// <summary>
    /// Gets a custom tag value from various tag formats (ID3v2 TXXX, Vorbis comments, MP4 atoms).
    /// </summary>
    private static string? GetCustomTag(TagLib.File file, string tagName)
    {
        // Try ID3v2 TXXX frames
        if (file.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag id3v2Tag)
        {
            // Check TXXX (User Text) frames first
            var txxxValue = GetId3v2UserTextFrame(id3v2Tag, tagName);
            if (!string.IsNullOrWhiteSpace(txxxValue))
            {
                return txxxValue;
            }

            // Check standard frames if the tag name matches a frame ID
            var frameValue = GetId3v2StandardFrame(id3v2Tag, tagName);
            if (!string.IsNullOrWhiteSpace(frameValue))
            {
                return frameValue;
            }
        }

        // Try Vorbis/FLAC comments
        if (file.GetTag(TagTypes.Xiph) is XiphComment vorbisTag)
        {
            var values = vorbisTag.GetField(tagName);
            if (values?.Length > 0)
            {
                return string.Join("; ", values.Where(v => !string.IsNullOrWhiteSpace(v)));
            }
        }

        // Try MP4/M4A atoms
        if (file.GetTag(TagTypes.Apple) is TagLib.Mpeg4.AppleTag appleTag)
        {
            var value = GetAppleTagValue(appleTag, tagName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        // Try APE tags
        if (file.GetTag(TagTypes.Ape) is TagLib.Ape.Tag apeTag)
        {
            var item = apeTag.GetItem(tagName);
            if (item != null)
            {
                return item.ToStringArray()?.FirstOrDefault();
            }
        }

        return null;
    }

    private static string? GetId3v2UserTextFrame(TagLib.Id3v2.Tag tag, string description)
    {
        return tag.GetFrames<UserTextInformationFrame>()
            .Where(frame => string.Equals(frame.Description, description, StringComparison.OrdinalIgnoreCase))
            .Select(frame => frame.Text?.FirstOrDefault())
            .FirstOrDefault(text => text is not null);
    }

    private static string? GetId3v2StandardFrame(TagLib.Id3v2.Tag tag, string frameId)
    {
        // Handle standard ID3v2 frame IDs (4 characters)
        if (frameId.Length == 4)
        {
            var frames = tag.GetFrames(new ReadOnlyByteVector(frameId));
            foreach (var frame in frames)
            {
                if (frame is TextInformationFrame textFrame)
                {
                    return textFrame.Text?.FirstOrDefault();
                }

                if (frame is UrlLinkFrame urlFrame)
                {
                    return urlFrame.Text?.FirstOrDefault();
                }
            }
        }

        return null;
    }

    private static string? GetAppleTagValue(TagLib.Mpeg4.AppleTag tag, string tagName)
    {
        // Map common tag names to iTunes atom names
        var atomName = tagName.ToUpperInvariant() switch
        {
            "BARCODE" => "----:com.apple.iTunes:BARCODE",
            "CATALOGNUMBER" => "----:com.apple.iTunes:CATALOGNUMBER",
            "LABEL" => "----:com.apple.iTunes:LABEL",
            "MEDIA" => "----:com.apple.iTunes:MEDIA",
            "RELEASETYPE" => "----:com.apple.iTunes:RELEASETYPE",
            "RELEASESTATUS" => "----:com.apple.iTunes:RELEASESTATUS",
            "RELEASECOUNTRY" => "----:com.apple.iTunes:RELEASECOUNTRY",
            "SCRIPT" => "----:com.apple.iTunes:SCRIPT",
            "ORIGINALDATE" => "----:com.apple.iTunes:ORIGINALDATE",
            "ORIGINALYEAR" => "----:com.apple.iTunes:ORIGINALYEAR",
            "DISCSUBTITLE" => "----:com.apple.iTunes:DISCSUBTITLE",
            "ISRC" => "----:com.apple.iTunes:ISRC",
            "WORK" => "----:com.apple.iTunes:WORK",
            "MOVEMENT" => "----:com.apple.iTunes:MOVEMENT",
            "MOVEMENTNUMBER" => "----:com.apple.iTunes:MOVEMENTNUMBER",
            "MOVEMENTTOTAL" => "----:com.apple.iTunes:MOVEMENTTOTAL",
            "SHOWMOVEMENT" => "shwm",
            "ACOUSTID_ID" => "----:com.apple.iTunes:Acoustid Id",
            "MUSICBRAINZ_TRACKID" => "----:com.apple.iTunes:MusicBrainz Track Id",
            "MUSICBRAINZ_RECORDINGID" => "----:com.apple.iTunes:MusicBrainz Track Id",
            "MUSICBRAINZ_WORKID" => "----:com.apple.iTunes:MusicBrainz Work Id",
            "MUSICBRAINZ_DISCID" => "----:com.apple.iTunes:MusicBrainz Disc Id",
            _ => $"----:com.apple.iTunes:{tagName}",
        };

        // Try the mapped atom name
        var dataBoxes = tag.DataBoxes(atomName);
        return dataBoxes
            .Select(box => box.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
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
