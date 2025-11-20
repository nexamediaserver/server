// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NetVips;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using ThumbHashes;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>Provides image transcoding (with cache) and persistence for artwork.</summary>
public sealed partial class ImageService : IImageService
{
    private const string PostersFolder = "posters";
    private const string BackdropsFolder = "backdrops";
    private const string LogosFolder = "logos";
    private const string ThumbnailsFolder = "thumbnails";
    private const string MetadataUriPrefix = "metadata://";
    private static readonly string PostersUriPrefix = $"{MetadataUriPrefix}{PostersFolder}";
    private static readonly string BackdropsUriPrefix = $"{MetadataUriPrefix}{BackdropsFolder}";
    private static readonly string LogosUriPrefix = $"{MetadataUriPrefix}{LogosFolder}";
    private static readonly string[] CommonImageExtensions = ["jpg", "jpeg", "png", "webp"];

    private readonly IApplicationPaths paths;
    private readonly HttpClient httpClient;
    private readonly ConcurrentDictionary<string, string?> resolvedPathCache = new();

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0052",
        Justification = "Used by LoggerMessage source generators."
    )]
    private readonly ILogger<ImageService> logger;

    private string? imageCacheDir;

    /// <summary>Initializes a new instance of the <see cref="ImageService"/> class.</summary>
    /// <param name="paths">Application paths service.</param>
    /// <param name="httpClient">HTTP client used to download remote artwork.</param>
    /// <param name="logger">Logger instance.</param>
    public ImageService(
        IApplicationPaths paths,
        HttpClient httpClient,
        ILogger<ImageService> logger
    )
    {
        this.paths = paths;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> GetOrTranscodeAsync(
        ImageTranscodeRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourcePath = this.ResolveUriToPathCached(request.SourceUri);
        if (sourcePath == null)
        {
            throw new FileNotFoundException("Source image not found", request.SourceUri);
        }

        var sourceExt = NormalizeExtension(Path.GetExtension(sourcePath));
        var ext = NormalizeExtension(request.Format) ?? sourceExt ?? "jpg";

        if (CanPassthrough(request, sourceExt, ext))
        {
            // Returning the original file avoids an unnecessary re-encode when no transforms are requested.
            return Task.FromResult(sourcePath);
        }

        var key = BuildCacheKey(request, ext);
        var cacheDir = this.GetImageCacheDirectory();
        var output = Path.Combine(cacheDir, $"{key}.{ext}");
        if (File.Exists(output))
        {
            this.LogCacheHit(output);
            return Task.FromResult(output);
        }

        int quality = Math.Clamp(request.Quality ?? 80, 1, 100);
        var saveOptions = CreateSaveOptions(ext, quality);

        if (this.TryFastThumbnail(sourcePath, request, saveOptions, output))
        {
            this.LogCacheMissWrote(output);
            return Task.FromResult(output);
        }

        using var image = Image.NewFromFile(sourcePath, access: NetVips.Enums.Access.Sequential);
        Image processed = image;
        try
        {
            processed = ResizeForRequest(image, request);
            processed.WriteToFile(output, saveOptions);
            this.LogCacheMissWrote(output);
        }
        finally
        {
            if (!ReferenceEquals(processed, image))
            {
                processed.Dispose();
            }
        }

        return Task.FromResult(output);
    }

    /// <inheritdoc/>
    public Task<string> SaveAgentArtworkAsync(
        MetadataItem item,
        string agentIdentifier,
        ArtworkKind kind,
        byte[] imageBytes,
        string? extension,
        CancellationToken cancellationToken
    )
    {
        var (folder, uriPrefix) = kind switch
        {
            ArtworkKind.Poster => (PostersFolder, PostersUriPrefix),
            ArtworkKind.Backdrop => (BackdropsFolder, BackdropsUriPrefix),
            ArtworkKind.Logo => (LogosFolder, LogosUriPrefix),
            _ => (PostersFolder, PostersUriPrefix),
        };
        var dir = this.GetMediaItemSubpath(item.Uuid, Path.Combine("art", folder));
        var safe = NormalizeName(agentIdentifier);
        var ext = NormalizeExtension(extension) ?? "jpg";
        var file = $"{safe}_{item.Uuid:N}.{ext}";
        var path = Path.Combine(dir, file);
        File.WriteAllBytes(path, imageBytes);
        this.LogSavedArtwork(path, kind.ToString());
        return Task.FromResult($"{uriPrefix}/{safe}_{item.Uuid:N}");
    }

    /// <inheritdoc/>
    public Task<string> SaveThumbnailAsync(
        Guid metadataUuid,
        string providerName,
        byte[] imageBytes,
        string? extension,
        CancellationToken cancellationToken
    )
    {
        var dir = this.GetMediaItemSubpath(metadataUuid, "thumbnails");
        var safe = NormalizeName(providerName);
        var ext = NormalizeExtension(extension) ?? "jpg";
        var file = $"{safe}_{metadataUuid:N}.{ext}";
        var path = Path.Combine(dir, file);
        File.WriteAllBytes(path, imageBytes);
        this.LogSavedThumbnail(path);
        return Task.FromResult($"metadata://thumbnails/{safe}_{metadataUuid:N}");
    }

    /// <inheritdoc/>
    public Task<string?> SetPrimaryArtworkAsync(
        MetadataItem item,
        ArtworkKind kind,
        IReadOnlyList<string> orderedAgentIdentifiers,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (folder, uriPrefix) = kind switch
        {
            ArtworkKind.Poster => (PostersFolder, PostersUriPrefix),
            ArtworkKind.Backdrop => (BackdropsFolder, BackdropsUriPrefix),
            ArtworkKind.Logo => (LogosFolder, LogosUriPrefix),
            _ => (PostersFolder, PostersUriPrefix),
        };
        var artDir = this.GetMediaItemSubpath(item.Uuid, Path.Combine("art", folder));
        var thumbDir = this.GetMediaItemSubpath(item.Uuid, ThumbnailsFolder);
        var uuid = item.Uuid.ToString("N");
        var primary = this.SelectArtworkFromAgents(
            item,
            kind,
            uriPrefix,
            artDir,
            uuid,
            orderedAgentIdentifiers
        );
        if (primary != null)
        {
            return Task.FromResult<string?>(primary);
        }

        if (kind == ArtworkKind.Poster)
        {
            var posterFallback = this.SelectAnyPoster(item, uriPrefix, artDir, uuid);
            if (posterFallback != null)
            {
                return Task.FromResult<string?>(posterFallback);
            }

            var thumbSelection = this.SelectPosterFromThumbnails(
                item,
                thumbDir,
                uuid,
                orderedAgentIdentifiers
            );
            if (thumbSelection != null)
            {
                return Task.FromResult<string?>(thumbSelection);
            }
        }

        this.LogPrimaryArtworkMissing(uuid, kind.ToString());
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<string?> ComputeThumbHashAsync(
        string sourceUri,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = this.ResolveUriToPath(sourceUri) ?? sourceUri;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Task.FromResult<string?>(null);
        }

        using var image = Image.NewFromFile(path, access: NetVips.Enums.Access.Sequential);
        var prepared = ResizeForThumbHash(image);
        var rgba = ToRgba(prepared);
        try
        {
            var bytes = rgba.WriteToMemory();
            var thumb = ThumbHash.FromImage(rgba.Width, rgba.Height, bytes);
            var hash = Convert.ToBase64String(thumb.Hash.ToArray());
            return Task.FromResult<string?>(hash);
        }
        finally
        {
            if (!ReferenceEquals(rgba, prepared))
            {
                rgba.Dispose();
            }

            if (!ReferenceEquals(prepared, image))
            {
                prepared.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task<string?> IngestExternalArtworkAsync(
        MetadataItem item,
        string sourceIdentifier,
        ArtworkKind kind,
        string source,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var data = await this.TryReadSourceAsync(source, cancellationToken).ConfigureAwait(false);
        if (data is null)
        {
            this.LogArtworkIngestFailed(source, kind.ToString(), "ReadFailed");
            return null;
        }

        var (bytes, extension) = data.Value;
        var ext = NormalizeExtension(extension) ?? "jpg";
        try
        {
            var uri = await this.SaveAgentArtworkAsync(
                    item,
                    sourceIdentifier,
                    kind,
                    bytes,
                    ext,
                    cancellationToken
                )
                .ConfigureAwait(false);

            return uri;
        }
        catch (Exception ex)
        {
            this.LogArtworkIngestError(source, kind.ToString(), ex.Message);
            return null;
        }
    }

    private static Image ResizeForThumbHash(Image image)
    {
        const int maxSide = 100;
        var largestSide = Math.Max(image.Width, image.Height);

        if (largestSide <= maxSide)
        {
            return image;
        }

        var scale = (double)maxSide / largestSide;
        var resized = image.Resize(scale);

        // Guard against rounding leaving a side slightly above the limit.
        if (resized.Width <= maxSide && resized.Height <= maxSide)
        {
            return resized;
        }

        var cropped = resized.Crop(
            0,
            0,
            Math.Min(resized.Width, maxSide),
            Math.Min(resized.Height, maxSide)
        );
        if (!ReferenceEquals(cropped, resized))
        {
            resized.Dispose();
        }

        return cropped;
    }

    // Static helpers first (StyleCop)
    private static string BuildCacheKey(ImageTranscodeRequest req, string extension)
    {
        var seed =
            $"v3|{req.SourceUri}|{req.Width}|{req.Height}|{extension}|{req.Quality}|{req.PreserveAspectRatio}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool CanPassthrough(
        ImageTranscodeRequest request,
        string? sourceExtension,
        string targetExtension
    )
    {
        return !request.Width.HasValue
            && !request.Height.HasValue
            && !request.Quality.HasValue
            && string.Equals(sourceExtension, targetExtension, StringComparison.Ordinal);
    }

    private static VOption CreateSaveOptions(string extension, int quality)
    {
        var options = new VOption();
        if (extension is "jpg" or "jpeg" or "webp" or "avif")
        {
            options.Add("Q", quality);
        }

        options.Add("strip", true);
        return options;
    }

    private static string NormalizeName(string value)
    {
        var cleaned = new string(
            value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray()
        );
        return cleaned.Length == 0 ? "unknown" : cleaned.ToLowerInvariant();
    }

    private static string? NormalizeExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return null;
        }

        ext = ext.Trim().TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "jpeg" => "jpg",
            _ => ext,
        };
    }

    private static string ExtensionFromContentType(string? mediaType)
    {
        return mediaType?.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/avif" => "avif",
            "image/heic" => "heic",
            "image/heif" => "heif",
            _ => "jpeg",
        };
    }

    private static Image ResizeForRequest(Image image, ImageTranscodeRequest request)
    {
        if (!request.Width.HasValue && !request.Height.HasValue)
        {
            return image;
        }

        var (targetWidth, targetHeight) = ResolveTargetDimensions(image, request);

        if (!request.PreserveAspectRatio)
        {
            return ResizeWithoutAspect(image, targetWidth, targetHeight);
        }

        if (request.Width.HasValue && request.Height.HasValue)
        {
            return ResizeCover(image, targetWidth, targetHeight);
        }

        if (request.Width.HasValue)
        {
            return image.ThumbnailImage(targetWidth);
        }

        int derivedWidth = (int)Math.Round(image.Width * (targetHeight / (double)image.Height));
        return image.ThumbnailImage(Math.Max(1, derivedWidth));
    }

    private static (int TargetWidth, int TargetHeight) ResolveTargetDimensions(
        Image image,
        ImageTranscodeRequest request
    )
    {
        int targetWidth = request.Width ?? image.Width;
        int targetHeight = request.Height ?? image.Height;
        return (targetWidth, targetHeight);
    }

    private static Image ResizeCover(Image image, int targetWidth, int targetHeight)
    {
        double scale = Math.Max(
            (double)targetWidth / image.Width,
            (double)targetHeight / image.Height
        );

        var scaled = image.Resize(scale);
        int left = Math.Max(0, (scaled.Width - targetWidth) / 2);
        int top = Math.Max(0, (scaled.Height - targetHeight) / 2);
        var cropped = scaled.Crop(left, top, targetWidth, targetHeight);
        if (!ReferenceEquals(cropped, scaled))
        {
            scaled.Dispose();
        }

        return cropped;
    }

    private static Image ResizeWithoutAspect(Image image, int targetWidth, int targetHeight)
    {
        double scale = (double)targetWidth / image.Width;
        var resized = image.Resize(scale);
        if (resized.Height == targetHeight)
        {
            return resized;
        }

        var embedded = resized.Embed(
            0,
            0,
            targetWidth,
            targetHeight,
            extend: NetVips.Enums.Extend.Black
        );
        if (!ReferenceEquals(embedded, resized))
        {
            resized.Dispose();
        }

        return embedded;
    }

    private static Image ToRgba(Image source)
    {
        var rgba = source.Colourspace(NetVips.Enums.Interpretation.Srgb);

        if (!rgba.HasAlpha())
        {
            rgba = rgba.AddAlpha();
        }

        if (rgba.Bands > 4)
        {
            rgba = rgba.ExtractBand(0, 4);
        }

        return rgba.Cast(NetVips.Enums.BandFormat.Uchar);
    }

    private bool TryFastThumbnail(
        string sourcePath,
        ImageTranscodeRequest request,
        VOption saveOptions,
        string outputPath
    )
    {
        if (!request.PreserveAspectRatio)
        {
            return false;
        }

        int targetWidth = request.Width ?? 0;
        int targetHeight = request.Height ?? 0;
        if (targetWidth == 0 && targetHeight == 0)
        {
            return false;
        }

        int sourceWidth;
        int sourceHeight;
        try
        {
            using var header = Image.NewFromFile(
                sourcePath,
                access: NetVips.Enums.Access.Sequential
            );
            sourceWidth = header.Width;
            sourceHeight = header.Height;
        }
        catch (VipsException ex)
        {
            this.LogThumbnailHeaderReadFailure(sourcePath, ex);
            return false;
        }

        if (targetWidth == 0)
        {
            targetWidth = Math.Max(
                1,
                (int)Math.Round(sourceWidth * (targetHeight / (double)sourceHeight))
            );
        }
        else if (targetHeight == 0)
        {
            targetHeight = Math.Max(
                1,
                (int)Math.Round(sourceHeight * (targetWidth / (double)sourceWidth))
            );
        }

        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return false;
        }

        var isUpscale = targetWidth > sourceWidth || targetHeight > sourceHeight;
        if (isUpscale)
        {
            return false;
        }

        try
        {
            using var thumb = Image.Thumbnail(
                sourcePath,
                targetWidth,
                targetHeight,
                size: request.Width.HasValue && request.Height.HasValue
                    ? Enums.Size.Both
                    : Enums.Size.Down,
                crop: request.Width.HasValue && request.Height.HasValue
                    ? Enums.Interesting.Centre
                    : Enums.Interesting.None
            );

            thumb.WriteToFile(outputPath, saveOptions);
            return true;
        }
        catch (VipsException ex)
        {
            this.LogThumbnailFastPathFailed(sourcePath, ex);
            return false;
        }
    }

    private string GetImageCacheDirectory()
    {
        if (this.imageCacheDir != null)
        {
            return this.imageCacheDir;
        }

        var cacheDir = Path.Combine(this.paths.CacheDirectory, "images");
        this.paths.EnsureDirectoryExists(cacheDir);
        this.imageCacheDir = cacheDir;
        return cacheDir;
    }

    private string? ResolveUriToPathCached(string uri)
    {
        return this.resolvedPathCache.GetOrAdd(uri, this.ResolveUriToPath);
    }

    private string? ResolveUriToPath(string uri)
    {
        if (!uri.StartsWith("metadata://", StringComparison.OrdinalIgnoreCase))
        {
            // For absolute paths, verify existence and return
            return File.Exists(uri) ? uri : null;
        }

        var tail = uri.Substring("metadata://".Length);
        var slash = tail.IndexOf('/');
        if (slash <= 0)
        {
            return null;
        }

        var type = tail[..slash];
        var name = tail[(slash + 1)..];
        var parts = name.Split('_', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        var uuid = parts[1];
        if (uuid.Length < 2)
        {
            return null;
        }

        var shard = uuid[..2];
        var baseDir = Path.Combine(this.paths.MediaDirectory, shard, uuid);
        var relative = type switch
        {
            PostersFolder => Path.Combine("art", PostersFolder),
            BackdropsFolder => Path.Combine("art", BackdropsFolder),
            LogosFolder => Path.Combine("art", LogosFolder),
            ThumbnailsFolder => ThumbnailsFolder,
            _ => string.Empty,
        };
        var targetDir = Path.Combine(baseDir, relative);

        // Try common extensions directly instead of directory enumeration
        string baseName = name.StartsWith("combined_", StringComparison.OrdinalIgnoreCase)
            ? "combined"
            : name;

        foreach (var ext in CommonImageExtensions)
        {
            var candidate = Path.Combine(targetDir, $"{baseName}.{ext}");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Fallback to directory enumeration only if direct lookup fails
        if (!System.IO.Directory.Exists(targetDir))
        {
            return null;
        }

        var pattern = name.StartsWith("combined_", StringComparison.OrdinalIgnoreCase)
            ? "combined.*"
            : name + ".*";

        return System.IO.Directory.EnumerateFiles(targetDir, pattern).FirstOrDefault();
    }

    private string? SelectAnyPoster(MetadataItem item, string uriPrefix, string artDir, string uuid)
    {
        if (!System.IO.Directory.Exists(artDir))
        {
            return null;
        }

        var anyPoster = System
            .IO.Directory.EnumerateFiles(artDir, $"*_{uuid}.*")
            .OrderBy(f => f)
            .FirstOrDefault();

        if (anyPoster == null)
        {
            return null;
        }

        var nameWithoutExt = Path.GetFileNameWithoutExtension(anyPoster);
        if (string.IsNullOrWhiteSpace(nameWithoutExt))
        {
            return null;
        }

        var baseUri = $"{uriPrefix}/{nameWithoutExt}";
        return this.ApplySelection(item, ArtworkKind.Poster, baseUri);
    }

    private string? SelectArtworkFromAgents(
        MetadataItem item,
        ArtworkKind kind,
        string uriPrefix,
        string artDir,
        string uuid,
        IReadOnlyList<string> orderedAgents
    )
    {
        foreach (var agent in orderedAgents)
        {
            var safe = NormalizeName(agent);
            var match = System
                .IO.Directory.EnumerateFiles(artDir, $"{safe}_{uuid}.*")
                .FirstOrDefault();
            if (match == null)
            {
                continue;
            }

            var baseUri = $"{uriPrefix}/{safe}_{uuid}";
            return this.ApplySelection(item, kind, baseUri);
        }

        return null;
    }

    private string? SelectPosterFromThumbnails(
        MetadataItem item,
        string thumbDir,
        string uuid,
        IReadOnlyList<string> orderedAgents
    )
    {
        if (!System.IO.Directory.Exists(thumbDir))
        {
            return null;
        }

        foreach (var agent in orderedAgents)
        {
            var safe = NormalizeName(agent);
            var thumb = System
                .IO.Directory.EnumerateFiles(thumbDir, $"{safe}_{uuid}.*")
                .FirstOrDefault();
            if (thumb != null)
            {
                var provider = Path.GetFileName(thumb)!.Split('_')[0];
                var baseUri = $"metadata://thumbnails/{provider}_{uuid}";
                return this.ApplySelection(item, ArtworkKind.Poster, baseUri);
            }
        }

        var anyThumb = System
            .IO.Directory.EnumerateFiles(thumbDir, $"*_{uuid}.*")
            .OrderBy(f => f)
            .FirstOrDefault();
        if (anyThumb != null)
        {
            var provider = Path.GetFileName(anyThumb)!.Split('_')[0];
            var baseUri = $"metadata://thumbnails/{provider}_{uuid}";
            return this.ApplySelection(item, ArtworkKind.Poster, baseUri);
        }

        return null;
    }

    private string? ApplySelection(MetadataItem item, ArtworkKind kind, string baseUri)
    {
        switch (kind)
        {
            case ArtworkKind.Poster:
                item.ThumbUri = baseUri;
                break;
            case ArtworkKind.Backdrop:
                item.ArtUri = baseUri;
                break;
            case ArtworkKind.Logo:
                item.LogoUri = baseUri;
                break;
        }

        this.LogPrimaryArtworkSelected(baseUri);
        return baseUri;
    }

    private string GetMediaItemSubpath(Guid uuid, string subfolder)
    {
        var id = uuid.ToString("N");
        var shard = id[..2];
        var root = Path.Combine(this.paths.MediaDirectory, shard, id);
        var full = Path.Combine(root, subfolder);
        this.paths.EnsureDirectoryExists(full);
        return full;
    }

    private async Task<(byte[] Bytes, string? Extension)?> TryReadSourceAsync(
        string source,
        CancellationToken cancellationToken
    )
    {
        if (
            Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        )
        {
            try
            {
                using var response = await this
                    .httpClient.GetAsync(uri, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var bytes = await response
                    .Content.ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var ext =
                    NormalizeExtension(ExtensionFromContentType(contentType))
                    ?? NormalizeExtension(Path.GetExtension(uri.AbsolutePath))
                    ?? "jpg";
                return (bytes, ext);
            }
            catch
            {
                return null;
            }
        }

        try
        {
            if (!Path.IsPathRooted(source) || !File.Exists(source))
            {
                return null;
            }

            var ext = NormalizeExtension(Path.GetExtension(source)) ?? "jpg";
            var bytes = await File.ReadAllBytesAsync(source, cancellationToken)
                .ConfigureAwait(false);
            return (bytes, ext);
        }
        catch
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Image cache hit: {Path}")]
    private partial void LogCacheHit(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Image cache miss, wrote: {Path}")]
    private partial void LogCacheMissWrote(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to read image header for {Source}")]
    private partial void LogThumbnailHeaderReadFailure(string source, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Thumbnail fast path failed for {Source}")]
    private partial void LogThumbnailFastPathFailed(string source, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saved artwork {Kind} to {Path}")]
    private partial void LogSavedArtwork(string path, string kind);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saved thumbnail to {Path}")]
    private partial void LogSavedThumbnail(string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Primary artwork selected: {Uri}")]
    private partial void LogPrimaryArtworkSelected(string uri);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Primary artwork not found for {Uuid} ({Kind})"
    )]
    private partial void LogPrimaryArtworkMissing(string uuid, string kind);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Artwork ingest failed from {Source} ({Kind}): {Reason}"
    )]
    private partial void LogArtworkIngestFailed(string source, string kind, string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Artwork ingest error from {Source} ({Kind}): {Reason}"
    )]
    private partial void LogArtworkIngestError(string source, string kind, string reason);
}
