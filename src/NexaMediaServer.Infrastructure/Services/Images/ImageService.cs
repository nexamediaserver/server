// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NetVips;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>Provides image transcoding (with cache) and persistence for artwork.</summary>
public sealed partial class ImageService : IImageService
{
    private readonly IApplicationPaths paths;
    private readonly ILogger<ImageService> logger;

    /// <summary>Initializes a new instance of the <see cref="ImageService"/> class.</summary>
    /// <param name="paths">Application paths service.</param>
    /// <param name="logger">Logger instance.</param>
    public ImageService(IApplicationPaths paths, ILogger<ImageService> logger)
    {
        this.paths = paths;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> GetOrTranscodeAsync(
        ImageTranscodeRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildCacheKey(request);
        var ext = NormalizeExtension(request.Format) ?? "jpg";
        var cacheDir = Path.Combine(this.paths.CacheDirectory, "images");
        this.paths.EnsureDirectoryExists(cacheDir);
        var output = Path.Combine(cacheDir, $"{key}.{ext}");
        if (File.Exists(output))
        {
            this.LogCacheHit(output);
            return Task.FromResult(output);
        }

        var sourcePath = this.ResolveUriToPath(request.SourceUri);
        if (sourcePath == null || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source image not found", request.SourceUri);
        }

        using var image = Image.NewFromFile(sourcePath, access: NetVips.Enums.Access.Sequential);
        Image processed = image;
        try
        {
            if (request.Width.HasValue || request.Height.HasValue)
            {
                int w = request.Width ?? image.Width;
                int h = request.Height ?? image.Height;
                if (request.PreserveAspectRatio)
                {
                    int dim = Math.Max(w, h);
                    processed = image.ThumbnailImage(dim);
                }
                else
                {
                    double scale = (double)w / image.Width;
                    processed = image.Resize(scale);
                    if (processed.Height != h)
                    {
                        processed = processed.Embed(0, 0, w, h, extend: NetVips.Enums.Extend.Black);
                    }
                }
            }

            int quality = Math.Clamp(request.Quality ?? 80, 1, 100);
            var v = new VOption();
            if (ext is "jpg" or "jpeg" or "webp" or "avif")
            {
                v.Add("Q", quality);
            }

            processed.WriteToFile(output, v);
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
            ArtworkKind.Poster => ("posters", "metadata://posters"),
            ArtworkKind.Backdrop => ("backdrops", "metadata://backdrops"),
            ArtworkKind.Logo => ("logos", "metadata://logos"),
            _ => ("posters", "metadata://posters"),
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
        MetadataItem item,
        string providerName,
        byte[] imageBytes,
        string? extension,
        CancellationToken cancellationToken
    )
    {
        var dir = this.GetMediaItemSubpath(item.Uuid, "thumbnails");
        var safe = NormalizeName(providerName);
        var ext = NormalizeExtension(extension) ?? "jpg";
        var file = $"{safe}_{item.Uuid:N}.{ext}";
        var path = Path.Combine(dir, file);
        File.WriteAllBytes(path, imageBytes);
        this.LogSavedThumbnail(path);
        return Task.FromResult($"metadata://thumbnails/{safe}_{item.Uuid:N}");
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
            ArtworkKind.Poster => ("posters", "metadata://posters"),
            ArtworkKind.Backdrop => ("backdrops", "metadata://backdrops"),
            ArtworkKind.Logo => ("logos", "metadata://logos"),
            _ => ("posters", "metadata://posters"),
        };
        var artDir = this.GetMediaItemSubpath(item.Uuid, Path.Combine("art", folder));
        var thumbDir = this.GetMediaItemSubpath(item.Uuid, "thumbnails");
        var uuid = item.Uuid.ToString("N");
        string? selectedUri = null;
        foreach (var agent in orderedAgentIdentifiers)
        {
            var safe = NormalizeName(agent);
            var match = System
                .IO.Directory.EnumerateFiles(artDir, $"{safe}_{uuid}.*")
                .FirstOrDefault();
            if (match != null)
            {
                selectedUri = $"{uriPrefix}/{safe}_{uuid}";
                // Set the appropriate URI field on the item based on artwork kind
                switch (kind)
                {
                    case ArtworkKind.Poster:
                        item.ThumbUri = selectedUri;
                        break;
                    case ArtworkKind.Backdrop:
                        item.ArtUri = selectedUri;
                        break;
                    case ArtworkKind.Logo:
                        item.LogoUri = selectedUri;
                        break;
                }

                this.LogPrimaryArtworkSelected(selectedUri);
                return Task.FromResult<string?>(selectedUri);
            }
        }

        // Thumbnails can be used as posters if no agent poster is found. Respect provider priority order.
        if (kind == ArtworkKind.Poster && System.IO.Directory.Exists(thumbDir))
        {
            foreach (var agent in orderedAgentIdentifiers)
            {
                var safe = NormalizeName(agent);
                var thumb = System
                    .IO.Directory.EnumerateFiles(thumbDir, $"{safe}_{uuid}.*")
                    .FirstOrDefault();
                if (thumb != null)
                {
                    var provider = Path.GetFileName(thumb)!.Split('_')[0];
                    selectedUri = $"metadata://thumbnails/{provider}_{uuid}";
                    item.ThumbUri = selectedUri; // set poster from thumbnail
                    this.LogPrimaryArtworkSelected(selectedUri);
                    return Task.FromResult<string?>(selectedUri);
                }
            }

            // Final fallback: if none matched by priority, pick any available thumbnail (previous behavior)
            var anyThumb = System
                .IO.Directory.EnumerateFiles(thumbDir, $"*_{uuid}.*")
                .OrderBy(f => f)
                .FirstOrDefault();
            if (anyThumb != null)
            {
                var provider = Path.GetFileName(anyThumb)!.Split('_')[0];
                selectedUri = $"metadata://thumbnails/{provider}_{uuid}";
                item.ThumbUri = selectedUri;
                this.LogPrimaryArtworkSelected(selectedUri);
                return Task.FromResult<string?>(selectedUri);
            }
        }

        this.LogPrimaryArtworkMissing(uuid, kind.ToString());
        return Task.FromResult<string?>(null);
    }

    // Static helpers first (StyleCop)
    private static string BuildCacheKey(ImageTranscodeRequest req)
    {
        var seed =
            $"{req.SourceUri}|{req.Width}|{req.Height}|{req.Format}|{req.Quality}|{req.PreserveAspectRatio}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash).ToLowerInvariant();
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

    private string? ResolveUriToPath(string uri)
    {
        if (!uri.StartsWith("metadata://", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
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
            "posters" => Path.Combine("art", "posters"),
            "backdrops" => Path.Combine("art", "backdrops"),
            "logos" => Path.Combine("art", "logos"),
            "thumbnails" => "thumbnails",
            _ => string.Empty,
        };
        var targetDir = Path.Combine(baseDir, relative);

        return !System.IO.Directory.Exists(targetDir)
            ? null
            : System.IO.Directory.EnumerateFiles(targetDir, name + ".*").FirstOrDefault();
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Image cache hit: {Path}")]
    private partial void LogCacheHit(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Image cache miss, wrote: {Path}")]
    private partial void LogCacheMissWrote(string path);

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
}
