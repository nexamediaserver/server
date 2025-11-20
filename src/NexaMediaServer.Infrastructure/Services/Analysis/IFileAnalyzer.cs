// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Analyzes media files to extract technical metadata.
/// </summary>
public interface IFileAnalyzer
{
    /// <summary>
    /// Gets the human-readable name of the analyzer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the ordering priority. Lower values run first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Determines if this analyzer supports the given item.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>True if supported, otherwise false.</returns>
    bool Supports(MediaItem item);

    /// <summary>
    /// Analyzes the given media item and its parts to extract technical metadata.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <param name="metadata">The parent metadata item (tracked) for contextual analysis.</param>
    /// <param name="parts">The associated media parts (may be empty).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis result, or null if no relevant data was found.</returns>
    Task<FileAnalysisResult?> AnalyzeAsync(
        MediaItem item,
        MetadataItem metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Represents the result of a file analysis operation.
/// </summary>
public sealed class FileAnalysisResult
{
    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the file format (e.g. "mp4", "mkv").
    /// </summary>
    public string? FileFormat { get; init; }

    /// <summary>
    /// Gets the duration of the media.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets whether the file contains chapter markers.
    /// </summary>
    public bool? HasChapters { get; init; }

    /// <summary>
    /// Gets the number of chapters in the file.
    /// </summary>
    public int? ChapterCount { get; init; }

    /// <summary>
    /// Gets whether the item is an optical disc image.
    /// </summary>
    public bool? IsDisc { get; init; }

    /// <summary>
    /// Gets the disc title, if available.
    /// </summary>
    public string? DiscTitle { get; init; }

    /// <summary>
    /// Gets the disc identifier or volume ID.
    /// </summary>
    public string? DiscId { get; init; }

    /// <summary>
    /// Gets the primary video codec (e.g., h264, hevc).
    /// </summary>
    public string? VideoCodec { get; init; }

    /// <summary>
    /// Gets the video codec profile.
    /// </summary>
    public string? VideoCodecProfile { get; init; }

    /// <summary>
    /// Gets the video codec level.
    /// </summary>
    public string? VideoCodecLevel { get; init; }

    /// <summary>
    /// Gets the video bitrate in bits per second.
    /// </summary>
    public int? VideoBitrate { get; init; }

    /// <summary>
    /// Gets the video width in pixels.
    /// </summary>
    public int? VideoWidth { get; init; }

    /// <summary>
    /// Gets the video height in pixels.
    /// </summary>
    public int? VideoHeight { get; init; }

    /// <summary>
    /// Gets the display aspect ratio (e.g., "16:9").
    /// </summary>
    public string? VideoAspectRatio { get; init; }

    /// <summary>
    /// Gets the video frame rate in frames per second.
    /// </summary>
    public double? VideoFrameRate { get; init; }

    /// <summary>
    /// Gets whether the video is interlaced.
    /// </summary>
    public bool? VideoIsInterlaced { get; init; }

    /// <summary>
    /// Gets the video bit depth per component.
    /// </summary>
    public int? VideoBitDepth { get; init; }

    /// <summary>
    /// Gets the video color space (e.g., BT.709, BT.2020).
    /// </summary>
    public string? VideoColorSpace { get; init; }

    /// <summary>
    /// Gets the video color primaries.
    /// </summary>
    public string? VideoColorPrimaries { get; init; }

    /// <summary>
    /// Gets the video transfer characteristics.
    /// </summary>
    public string? VideoColorTransfer { get; init; }

    /// <summary>
    /// Gets the video color range (e.g., Limited, Full).
    /// </summary>
    public string? VideoColorRange { get; init; }

    /// <summary>
    /// Gets the video dynamic range (e.g., HDR10, Dolby Vision).
    /// </summary>
    public string? VideoDynamicRange { get; init; }

    /// <summary>
    /// Gets the distinct audio codecs present.
    /// </summary>
    public ICollection<string>? AudioCodecs { get; init; }

    /// <summary>
    /// Gets the audio track languages.
    /// </summary>
    public ICollection<string>? AudioLanguages { get; init; }

    /// <summary>
    /// Gets the audio channel counts.
    /// </summary>
    public ICollection<int>? AudioChannelCounts { get; init; }

    /// <summary>
    /// Gets the audio sample rates in hertz.
    /// </summary>
    public ICollection<int>? AudioSampleRates { get; init; }

    /// <summary>
    /// Gets the audio bit depths.
    /// </summary>
    public ICollection<int>? AudioBitDepths { get; init; }

    /// <summary>
    /// Gets the audio bitrates in bits per second.
    /// </summary>
    public ICollection<int>? AudioBitrates { get; init; }

    /// <summary>
    /// Gets the total number of audio tracks.
    /// </summary>
    public int? AudioTrackCount { get; init; }

    /// <summary>
    /// Gets the total number of subtitle tracks.
    /// </summary>
    public int? SubtitleTrackCount { get; init; }

    /// <summary>
    /// Gets the subtitle languages.
    /// </summary>
    public ICollection<string>? SubtitleLanguages { get; init; }

    /// <summary>
    /// Gets the subtitle formats (e.g., srt, pgs).
    /// </summary>
    public ICollection<string>? SubtitleFormats { get; init; }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int? ImageWidth { get; init; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int? ImageHeight { get; init; }

    /// <summary>
    /// Gets the image bit depth.
    /// </summary>
    public int? ImageBitDepth { get; init; }

    /// <summary>
    /// Gets the image color space.
    /// </summary>
    public string? ImageColorSpace { get; init; }

    /// <summary>
    /// Gets whether the image contains EXIF metadata.
    /// </summary>
    public bool? ImageHasExif { get; init; }

    /// <summary>
    /// Gets the date and time the image was taken, if available.
    /// </summary>
    public DateTimeOffset? ImageDateTaken { get; init; }

    /// <summary>
    /// Gets the number of pages in the document.
    /// </summary>
    public int? DocumentPageCount { get; init; }

    /// <summary>
    /// Gets the word count of the document.
    /// </summary>
    public int? DocumentWordCount { get; init; }

    /// <summary>
    /// Gets whether the document contains images.
    /// </summary>
    public bool? DocumentHasImages { get; init; }

    /// <summary>
    /// Gets the game target platforms.
    /// </summary>
    public ICollection<string>? GamePlatforms { get; init; }

    /// <summary>
    /// Gets the game regions.
    /// </summary>
    public ICollection<string>? GameRegions { get; init; }

    /// <summary>
    /// Gets whether the game is a ROM file.
    /// </summary>
    public bool? GameIsRom { get; init; }
}
