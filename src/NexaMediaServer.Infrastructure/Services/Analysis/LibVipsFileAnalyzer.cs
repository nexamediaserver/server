// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

using NetVips;

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

using VipsImage = NetVips.Image;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// File analyzer using LibVips to extract technical metadata from image files.
/// </summary>
/// <remarks>
/// This analyzer extracts image dimensions, format, color space, and EXIF metadata
/// from photos and pictures using the LibVips library.
/// </remarks>
public partial class LibVipsFileAnalyzer : IFileAnalyzer<Photo>, IFileAnalyzer<Picture>
{
    private const long MaxDecodedPixels = 120_000_000; // ~12k x 10k
    private readonly ILogger<LibVipsFileAnalyzer> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibVipsFileAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LibVipsFileAnalyzer(ILogger<LibVipsFileAnalyzer> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public string Name => "LibVips Image Analyzer";

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    bool IFileAnalyzer<Photo>.Supports(MediaItem item, Photo metadata) =>
        SupportsInternal(item);

    /// <inheritdoc />
    bool IFileAnalyzer<Picture>.Supports(MediaItem item, Picture metadata) =>
        SupportsInternal(item);

    /// <inheritdoc />
    Task<FileAnalysisResult?> IFileAnalyzer<Photo>.AnalyzeAsync(
        MediaItem item,
        Photo metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.AnalyzeInternalAsync(item, parts, cancellationToken);

    /// <inheritdoc />
    Task<FileAnalysisResult?> IFileAnalyzer<Picture>.AnalyzeAsync(
        MediaItem item,
        Picture metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.AnalyzeInternalAsync(item, parts, cancellationToken);

    private static bool SupportsInternal(MediaItem? item)
    {
        if (item == null)
        {
            return false;
        }

        var firstPart = item.Parts?.FirstOrDefault();
        if (firstPart == null || string.IsNullOrWhiteSpace(firstPart.File))
        {
            return false;
        }

        var ext = Path.GetExtension(firstPart.File);
        return MediaFileExtensions.IsImage(ext);
    }

    /// <summary>
    /// Gets the bit depth per component from the image.
    /// </summary>
    private static int? GetBitDepth(VipsImage image)
    {
        try
        {
            // VipsFormat indicates bits per band
            return image.Format switch
            {
                NetVips.Enums.BandFormat.Uchar => 8,
                NetVips.Enums.BandFormat.Char => 8,
                NetVips.Enums.BandFormat.Ushort => 16,
                NetVips.Enums.BandFormat.Short => 16,
                NetVips.Enums.BandFormat.Uint => 32,
                NetVips.Enums.BandFormat.Int => 32,
                NetVips.Enums.BandFormat.Float => 32,
                NetVips.Enums.BandFormat.Double => 64,
                NetVips.Enums.BandFormat.Complex => 64,
                NetVips.Enums.BandFormat.Dpcomplex => 128,
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the color space interpretation from the image.
    /// </summary>
    private static string? GetColorSpace(VipsImage image)
    {
        try
        {
            return image.Interpretation switch
            {
                NetVips.Enums.Interpretation.Srgb => "sRGB",
                NetVips.Enums.Interpretation.Rgb => "RGB",
                NetVips.Enums.Interpretation.Rgb16 => "RGB16",
                NetVips.Enums.Interpretation.Grey16 => "Grey16",
                NetVips.Enums.Interpretation.Bw => "B&W",
                NetVips.Enums.Interpretation.Cmyk => "CMYK",
                NetVips.Enums.Interpretation.Lab => "Lab",
                NetVips.Enums.Interpretation.Labq => "LabQ",
                NetVips.Enums.Interpretation.Lch => "LCH",
                NetVips.Enums.Interpretation.Xyz => "XYZ",
                NetVips.Enums.Interpretation.Hsv => "HSV",
                NetVips.Enums.Interpretation.Scrgb => "scRGB",
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the image contains EXIF metadata.
    /// </summary>
    private static bool? HasExif(VipsImage image)
    {
        try
        {
            // Check for common EXIF fields
            return image.Contains("exif-ifd0-Make")
                   || image.Contains("exif-ifd0-Model")
                   || image.Contains("exif-ifd0-DateTime")
                   || image.Contains("exif-ifd2-DateTimeOriginal");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the date the photo was taken from EXIF metadata.
    /// </summary>
    private static DateTimeOffset? GetDateTaken(VipsImage image)
    {
        try
        {
            // Try DateTimeOriginal first (when photo was actually taken)
            if (image.Contains("exif-ifd2-DateTimeOriginal"))
            {
                var dateStr = image.Get("exif-ifd2-DateTimeOriginal") as string;
                if (TryParseExifDate(dateStr, out var date))
                {
                    return date;
                }
            }

            // Fall back to DateTime (when file was created/modified in camera)
            if (image.Contains("exif-ifd0-DateTime"))
            {
                var dateStr = image.Get("exif-ifd0-DateTime") as string;
                if (TryParseExifDate(dateStr, out var date))
                {
                    return date;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses an EXIF date string (format: "YYYY:MM:DD HH:MM:SS").
    /// </summary>
    private static bool TryParseExifDate(string? dateStr, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return false;
        }

        // EXIF date format: "2024:12:30 15:30:00"
        var trimmed = dateStr.Trim();
        if (trimmed.Length >= 19)
        {
            // Replace EXIF date separators with standard format
            var normalized = trimmed[..10].Replace(':', '-') + trimmed[10..];
            if (DateTimeOffset.TryParse(
                normalized,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal,
                out result))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the file format from extension and image loader.
    /// </summary>
    private static string? GetFileFormat(string filePath, VipsImage image)
    {
        try
        {
            // Try to get format from VipsLoader
            if (image.Contains("vips-loader"))
            {
                var loader = image.Get("vips-loader") as string;
                if (!string.IsNullOrWhiteSpace(loader))
                {
                    // Loader names are like "jpegload", "pngload", etc.
                    return loader.Replace("load", string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Fall back to file extension
            var ext = Path.GetExtension(filePath)?.TrimStart('.').ToLowerInvariant();
            return string.IsNullOrEmpty(ext) ? null : ext;
        }
        catch
        {
            return Path.GetExtension(filePath)?.TrimStart('.').ToLowerInvariant();
        }
    }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    private static long? GetFileSize(string filePath)
    {
        try
        {
            var fi = new FileInfo(filePath);
            return fi.Exists ? fi.Length : null;
        }
        catch
        {
            return null;
        }
    }

    private Task<FileAnalysisResult?> AnalyzeInternalAsync(
        MediaItem item,
        IReadOnlyList<MediaPart>? parts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (parts == null || parts.Count == 0)
        {
            this.LogNoPartsForAnalysis(item.Id);
            return Task.FromResult<FileAnalysisResult?>(null);
        }

        var firstPart = parts[0];
        if (string.IsNullOrWhiteSpace(firstPart.File) || !File.Exists(firstPart.File))
        {
            this.LogMissingPartFile(item.Id, firstPart.File ?? "<null>");
            return Task.FromResult<FileAnalysisResult?>(null);
        }

        var ext = Path.GetExtension(firstPart.File);
        if (!MediaFileExtensions.IsImage(ext))
        {
            this.LogSkipNonImage(item.Id, ext);
            return Task.FromResult<FileAnalysisResult?>(null);
        }

        return Task.FromResult(this.AnalyzeImage(item, firstPart));
    }

    private FileAnalysisResult? AnalyzeImage(MediaItem item, MediaPart part)
    {
        this.LogAnalyzeStart(item.Id, part.File);

        try
        {
            var loadOptions = new VOption { { "autorotate", true } };

            using var image = VipsImage.NewFromFile(
                part.File,
                access: NetVips.Enums.Access.Sequential,
                failOn: NetVips.Enums.FailOn.Warning,
                revalidate: true,
                kwargs: loadOptions
            );

            var totalPixels = (long)image.Width * image.Height;
            if (totalPixels > MaxDecodedPixels)
            {
                this.LogSkipImageTooLarge(item.Id, image.Width, image.Height, MaxDecodedPixels);
                return null;
            }

            var result = new FileAnalysisResult
            {
                ImageWidth = image.Width,
                ImageHeight = image.Height,
                ImageBitDepth = GetBitDepth(image),
                ImageColorSpace = GetColorSpace(image),
                ImageHasExif = HasExif(image),
                ImageDateTaken = GetDateTaken(image),
                FileFormat = GetFileFormat(part.File, image),
                FileSizeBytes = GetFileSize(part.File),
            };

            this.LogAnalyzeComplete(
                item.Id,
                result.ImageWidth ?? 0,
                result.ImageHeight ?? 0,
                result.FileFormat ?? "unknown"
            );

            return result;
        }
        catch (VipsException ex)
        {
            this.LogVipsError(ex, item.Id);
            return null;
        }
        catch (Exception ex)
        {
            this.LogGeneralError(ex, item.Id);
            return null;
        }
    }

    #region Logging

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analysis skip: no parts for media item {MediaItemId}"
    )]
    private partial void LogNoPartsForAnalysis(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analysis skip: part file missing for media item {MediaItemId} path={Path}"
    )]
    private partial void LogMissingPartFile(int MediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analysis skip: non-image media item {MediaItemId} ext={Extension}"
    )]
    private partial void LogSkipNonImage(int MediaItemId, string? Extension);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image analysis start: mediaItem={MediaItemId} path={Path}"
    )]
    private partial void LogAnalyzeStart(int MediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analysis skip: image too large for media item {MediaItemId} width={Width} height={Height} maxPixels={MaxPixels}"
    )]
    private partial void LogSkipImageTooLarge(int MediaItemId, int Width, int Height, long MaxPixels);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Image analysis complete: mediaItem={MediaItemId} width={Width} height={Height} format={Format}"
    )]
    private partial void LogAnalyzeComplete(int MediaItemId, int Width, int Height, string Format);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LibVips error analyzing item {MediaItemId}")]
    private partial void LogVipsError(Exception ex, int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "General error analyzing image for item {MediaItemId}"
    )]
    private partial void LogGeneralError(Exception ex, int MediaItemId);

    #endregion
}
