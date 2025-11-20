// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a media item with associated metadata and media parts.
/// </summary>
public class MediaItem : SoftDeletableEntity
{
    /// <summary>
    /// Gets or sets the metadata item identifier.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the associated metadata item.
    /// </summary>
    public MetadataItem MetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the section location identifier.
    /// </summary>
    public int SectionLocationId { get; set; }

    /// <summary>
    /// Gets or sets the associated section location.
    /// </summary>
    public SectionLocation SectionLocation { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of media parts.
    /// </summary>
    public ICollection<MediaPart> Parts { get; set; } = new List<MediaPart>();

    // -----------------------------------------------------------------------
    // Common / Aggregated Technical Characteristics
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the total (aggregated) file size in bytes across all parts.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the primary file/container/wrapper format (e.g. mkv, mp4, pdf, epub, cbz, iso, rom, chd).
    /// </summary>
    public string? FileFormat { get; set; }

    /// <summary>
    /// Gets or sets the total runtime/duration for time-based media (video, audio, podcasts, audiobooks, etc.).
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item has chapter/section markers.
    /// </summary>
    public bool? HasChapters { get; set; }

    /// <summary>
    /// Gets or sets the number of chapters/sections if available.
    /// </summary>
    public int? ChapterCount { get; set; }

    // -----------------------------------------------------------------------
    // Disc / Optical Media (DVD, Blu-ray, UHD Blu-ray, Audio CD, Game Disc)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether this item originates from a disc/optical structure.
    /// </summary>
    public bool IsDisc { get; set; }

    /// <summary>
    /// Gets or sets the disc type (e.g. DVD, BluRay, UHD, AudioCD, GameDisc).
    /// </summary>
    public DiscType? DiscType { get; set; }

    /// <summary>
    /// Gets or sets the disc title (volume label) if available.
    /// </summary>
    public string? DiscTitle { get; set; }

    /// <summary>
    /// Gets or sets an identifier for the disc (e.g. hashed volume ID, serial number).
    /// </summary>
    public string? DiscId { get; set; }

    // -----------------------------------------------------------------------
    // Video Characteristics (Movies, Shows, Episodes, Music Videos, Home Videos, etc.)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the video codec (e.g. h264, hevc, vp9, av1).
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the video codec profile (e.g. High, Main10, Main, Baseline).
    /// </summary>
    public string? VideoCodecProfile { get; set; }

    /// <summary>
    /// Gets or sets the video codec level if provided.
    /// </summary>
    public string? VideoCodecLevel { get; set; }

    /// <summary>
    /// Gets or sets the video bitrate in bits per second (aggregated or primary stream).
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the pixel width of the video.
    /// </summary>
    public int? VideoWidth { get; set; }

    /// <summary>
    /// Gets or sets the pixel height of the video.
    /// </summary>
    public int? VideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the video aspect ratio (stored as a string, e.g. "16:9", "2.39:1").
    /// </summary>
    public string? VideoAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the frame rate (frames per second). Fractional/NTSC rates can be approximated.
    /// </summary>
    public double? VideoFrameRate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is interlaced.
    /// </summary>
    public bool? VideoIsInterlaced { get; set; }

    /// <summary>
    /// Gets or sets the video bit depth (e.g. 8, 10, 12).
    /// </summary>
    public int? VideoBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the video color space (e.g. BT.709, BT.2020).
    /// </summary>
    public string? VideoColorSpace { get; set; }

    /// <summary>
    /// Gets or sets the video color primaries.
    /// </summary>
    public string? VideoColorPrimaries { get; set; }

    /// <summary>
    /// Gets or sets the video transfer characteristics (e.g. BT.709, PQ, HLG).
    /// </summary>
    public string? VideoColorTransfer { get; set; }

    /// <summary>
    /// Gets or sets the video color range (e.g. Limited, Full).
    /// </summary>
    public string? VideoColorRange { get; set; }

    /// <summary>
    /// Gets or sets the dynamic range classification (e.g. SDR, HDR10, DolbyVision, HLG).
    /// </summary>
    public string? VideoDynamicRange { get; set; }

    // -----------------------------------------------------------------------
    // Audio Characteristics (Music, Audiobooks, Podcasts, Tracks within Video, etc.)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets a collection of audio codecs present (one per audio track).
    /// </summary>
    public ICollection<string> AudioCodecs { get; } = new List<string>();

    /// <summary>
    /// Gets a collection of audio languages (ISO 639 codes) for tracks.
    /// </summary>
    public ICollection<string> AudioLanguages { get; } = new List<string>();

    /// <summary>
    /// Gets a collection of channel counts (e.g. 2, 6, 8) corresponding to audio tracks.
    /// </summary>
    public ICollection<int> AudioChannelCounts { get; } = new List<int>();

    /// <summary>
    /// Gets a collection of sample rates (Hz) corresponding to audio tracks.
    /// </summary>
    public ICollection<int> AudioSampleRates { get; } = new List<int>();

    /// <summary>
    /// Gets a collection of bit depths corresponding to audio tracks.
    /// </summary>
    public ICollection<int> AudioBitDepths { get; } = new List<int>();

    /// <summary>
    /// Gets a collection of audio bitrates (bits per second) corresponding to audio tracks.
    /// </summary>
    public ICollection<int> AudioBitrates { get; } = new List<int>();

    /// <summary>
    /// Gets or sets the number of audio tracks.
    /// </summary>
    public int? AudioTrackCount { get; set; }

    // -----------------------------------------------------------------------
    // Subtitle / Timed Text Characteristics
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the number of subtitle/timed text tracks.
    /// </summary>
    public int? SubtitleTrackCount { get; set; }

    /// <summary>
    /// Gets a collection of subtitle languages (ISO 639 codes).
    /// </summary>
    public ICollection<string> SubtitleLanguages { get; } = new List<string>();

    /// <summary>
    /// Gets a collection of subtitle formats (e.g. srt, ass, vtt, pgssub, dvbsub).
    /// </summary>
    public ICollection<string> SubtitleFormats { get; } = new List<string>();

    // -----------------------------------------------------------------------
    // Image / Photo / Picture Characteristics
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the pixel width for images/photos.
    /// </summary>
    public int? ImageWidth { get; set; }

    /// <summary>
    /// Gets or sets the pixel height for images/photos.
    /// </summary>
    public int? ImageHeight { get; set; }

    /// <summary>
    /// Gets or sets the image bit depth.
    /// </summary>
    public int? ImageBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the image color space (e.g. sRGB, AdobeRGB, DisplayP3).
    /// </summary>
    public string? ImageColorSpace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether EXIF/metadata is present.
    /// </summary>
    public bool? ImageHasExif { get; set; }

    /// <summary>
    /// Gets or sets the original capture date/time if available.
    /// </summary>
    public DateTimeOffset? ImageDateTaken { get; set; }

    // -----------------------------------------------------------------------
    // Document / Book / Comic Characteristics
    // -----------------------------------------------------------------------

    // DocumentFormat merged into FileFormat.

    /// <summary>
    /// Gets or sets the total page count (if applicable).
    /// </summary>
    public int? DocumentPageCount { get; set; }

    /// <summary>
    /// Gets or sets an estimated word count (if analyzable).
    /// </summary>
    public int? DocumentWordCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document contains embedded images.
    /// </summary>
    public bool? DocumentHasImages { get; set; }

    // -----------------------------------------------------------------------
    // Game / ROM / Software Characteristics
    // -----------------------------------------------------------------------

    // GameFileFormat merged into FileFormat.

    /// <summary>
    /// Gets a collection of target platforms (e.g. PC, PS2, Switch, SNES).
    /// </summary>
    public ICollection<string> GamePlatforms { get; } = new List<string>();

    /// <summary>
    /// Gets a collection of regions (e.g. NTSC-U, NTSC-J, PAL) associated with the game image/ROM.
    /// </summary>
    public ICollection<string> GameRegions { get; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the game file is a ROM (cartridge image) as opposed to an optical disc image.
    /// </summary>
    public bool? GameIsRom { get; set; }

    // GameIsDiscImage removed; use IsDisc + DiscType for disc images.
}
