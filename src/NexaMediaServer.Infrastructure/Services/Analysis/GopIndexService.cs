// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Default implementation for reading and writing GoP (Group of Pictures) XML indexes.
/// Uses a bounded in-memory LRU cache to reduce disk I/O for frequently accessed indexes.
/// </summary>
public partial class GopIndexService : IGopIndexService, IDisposable
{
    /// <summary>
    /// Maximum number of GoP indexes to keep in cache. Each index averages ~10-50KB depending
    /// on video length. With 128 entries, worst case is ~6.4MB which is acceptable.
    /// </summary>
    private const int MaxCacheEntries = 128;

    /// <summary>
    /// Sliding expiration for cached entries. Indexes not accessed within this window are evicted.
    /// </summary>
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(10);

    private readonly ILogger<GopIndexService> logger;
    private readonly IApplicationPaths paths;
    private readonly MemoryCache cache;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GopIndexService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="paths">Application paths utility.</param>
    public GopIndexService(ILogger<GopIndexService> logger, IApplicationPaths paths)
    {
        this.logger = logger;
        this.paths = paths;
        this.cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = MaxCacheEntries });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public string GetGopPath(Guid metadataUuid, int partIndex)
    {
        var uuid = metadataUuid.ToString("N");
        if (uuid.Length < 2)
        {
            throw new ArgumentException("Invalid UUID for metadata.", nameof(metadataUuid));
        }

        var shard = uuid[..2];
        var baseDir = Path.Combine(this.paths.MediaDirectory, shard, uuid);
        this.paths.EnsureDirectoryExists(baseDir);
        var gopFileName = $"GoP-{partIndex}.xml";
        return Path.Combine(baseDir, gopFileName);
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        Guid metadataUuid,
        int partIndex,
        GopIndex index,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(index);

        var gopPath = this.GetGopPath(metadataUuid, partIndex);

        var doc = new XDocument(
            new XElement(
                "VideoStream",
                new XAttribute(
                    "timebaseNumerator",
                    index.TimebaseNumerator.ToString(CultureInfo.InvariantCulture)
                ),
                new XAttribute(
                    "timebaseDenomenator",
                    index.TimebaseDenominator.ToString(CultureInfo.InvariantCulture)
                ),
                index.Groups.Select(g => new XElement(
                    "Group",
                    new XAttribute("pts", g.PtsMs),
                    new XAttribute("duration", g.DurationMs),
                    new XAttribute("size", g.SizeBytes)
                ))
            )
        );

        // Write directly to FileStream - no intermediate MemoryStream needed
        await using var fs = new FileStream(
            gopPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true
        );
        await doc.SaveAsync(fs, SaveOptions.None, cancellationToken).ConfigureAwait(false);

        // Invalidate cache entry after writing new index
        var cacheKey = GetCacheKey(metadataUuid, partIndex);
        this.cache.Remove(cacheKey);

        this.LogGopWritten(gopPath, index.Groups.Count);
    }

    /// <inheritdoc />
    public async Task<GopIndex?> TryReadAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = GetCacheKey(metadataUuid, partIndex);

        // Check cache first
        if (this.cache.TryGetValue(cacheKey, out GopIndex? cachedIndex))
        {
            this.LogGopCacheHit(cacheKey);
            return cachedIndex;
        }

        var gopPath = this.GetGopPath(metadataUuid, partIndex);

        if (!File.Exists(gopPath))
        {
            return null;
        }

        try
        {
            // Load XDocument directly from FileStream - avoids StreamReader and string allocation
            await using var fs = new FileStream(
                gopPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );
            var doc = await XDocument
                .LoadAsync(fs, LoadOptions.None, cancellationToken)
                .ConfigureAwait(false);

            var root = doc.Root;
            if (
                root == null
                || !string.Equals(root.Name.LocalName, "VideoStream", StringComparison.Ordinal)
            )
            {
                return null;
            }

            var index = new GopIndex();

            // Parse timebase attributes
            if (TryParseAttribute(root, "timebaseNumerator", out int num))
            {
                index.TimebaseNumerator = num;
            }

            if (TryParseAttribute(root, "timebaseDenomenator", out int den))
            {
                index.TimebaseDenominator = den;
            }

            // Pre-allocate capacity to reduce list reallocations
            var groups = root.Elements("Group").ToList();
            if (groups.Count > 0)
            {
                index.Groups.Capacity = groups.Count;
            }

            foreach (var g in groups)
            {
                if (!TryParseAttribute(g, "pts", out long pts))
                {
                    continue;
                }

                var dur = TryParseAttribute(g, "duration", out long d) ? d : 0L;
                var size = TryParseAttribute(g, "size", out long s) ? s : 0L;

                index.Groups.Add(new GopGroup(pts, dur, size));
            }

            // Cache the parsed index with sliding expiration and size = 1
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSlidingExpiration,
                Size = 1,
            };
            this.cache.Set(cacheKey, index, cacheOptions);
            this.LogGopCacheMiss(cacheKey);

            return index;
        }
        catch (Exception ex)
        {
            this.LogGopReadFailed(gopPath, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<GopIndex?> TryReadForPartAsync(
        MediaItem mediaItem,
        MediaPart mediaPart,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        ArgumentNullException.ThrowIfNull(mediaPart);
        var metadata =
            mediaItem.MetadataItem
            ?? throw new ArgumentException(
                "MediaItem.MetadataItem must be loaded.",
                nameof(mediaItem)
            );

        // Determine part index within the media item; if Parts not loaded, fall back to 0 when only one part known.
        int partIndex = 0;
        if (mediaItem.Parts is { Count: > 0 })
        {
            var list = mediaItem.Parts.ToList();
            partIndex = Math.Max(0, list.IndexOf(mediaPart));
            if (partIndex < 0)
            {
                // Not found; attempt to match by File path
                partIndex = list.FindIndex(p =>
                    string.Equals(p.File, mediaPart.File, StringComparison.OrdinalIgnoreCase)
                );
                if (partIndex < 0)
                {
                    partIndex = 0; // default
                }
            }
        }

        return this.TryReadAsync(metadata.Uuid, partIndex, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GopGroup?> GetNearestKeyframeAsync(
        Guid metadataUuid,
        int partIndex,
        long targetMs,
        CancellationToken cancellationToken
    )
    {
        var index = await this.TryReadAsync(metadataUuid, partIndex, cancellationToken)
            .ConfigureAwait(false);

        if (index == null || index.Groups.Count == 0)
        {
            return null;
        }

        // Binary search for the nearest keyframe at or before targetMs
        var groups = index.Groups;
        int left = 0;
        int right = groups.Count - 1;
        int result = 0;

        while (left <= right)
        {
            int mid = left + ((right - left) / 2);
            if (groups[mid].PtsMs <= targetMs)
            {
                result = mid;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return groups[result];
    }

    /// <inheritdoc />
    public bool HasGopIndex(Guid metadataUuid, int partIndex)
    {
        var cacheKey = GetCacheKey(metadataUuid, partIndex);
        if (this.cache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        var gopPath = this.GetGopPath(metadataUuid, partIndex);
        return File.Exists(gopPath);
    }

    /// <summary>
    /// Releases resources used by the service.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.cache.Dispose();
        }

        this.disposed = true;
    }

    /// <summary>
    /// Generates a unique cache key for the given metadata UUID and part index.
    /// </summary>
    private static string GetCacheKey(Guid metadataUuid, int partIndex)
    {
        return $"gop:{metadataUuid:N}:{partIndex}";
    }

    /// <summary>
    /// Helper method to parse an integer attribute value.
    /// </summary>
    private static bool TryParseAttribute(XElement element, string attributeName, out int value)
    {
        var attrValue = element.Attribute(attributeName)?.Value;
        if (attrValue != null)
        {
            return int.TryParse(
                attrValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Helper method to parse a long attribute value.
    /// </summary>
    private static bool TryParseAttribute(XElement element, string attributeName, out long value)
    {
        var attrValue = element.Attribute(attributeName)?.Value;
        if (attrValue != null)
        {
            return long.TryParse(
                attrValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        value = 0;
        return false;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Wrote GoP index: path={GopPath} groups={Count}"
    )]
    private partial void LogGopWritten(string gopPath, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to read GoP XML at {GopPath}")]
    private partial void LogGopReadFailed(string gopPath, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GoP cache hit: key={CacheKey}")]
    private partial void LogGopCacheHit(string cacheKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GoP cache miss: key={CacheKey}")]
    private partial void LogGopCacheMiss(string cacheKey);
}
