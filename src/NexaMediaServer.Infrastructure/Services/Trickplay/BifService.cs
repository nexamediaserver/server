// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Trickplay;

/// <summary>
/// Default implementation for reading and writing BIF (Base Index Frames) files.
/// BIF format specification from Roku:
/// - Magic number: 0x89424946 ("\x89BIF")
/// - Version: 4 bytes (little-endian int32)
/// - Frame count: 4 bytes (little-endian int32)
/// - Timestamp multiplier: 4 bytes (little-endian int32) - typically 1000 for milliseconds
/// - Reserved: 44 bytes (zeroed)
/// - Index entries: array of [timestamp (4 bytes), offset (4 bytes)] pairs
/// - Image data: concatenated JPEG images at their respective offsets.
/// </summary>
public partial class BifService : IBifService
{
    private const uint BifMagicNumber = 0x89424946; // "\x89BIF"
    private const int BifHeaderSize = 64; // Total header size in bytes
    private const int BifEntrySize = 8; // 4 bytes timestamp + 4 bytes offset

    private readonly ILogger<BifService> logger;
    private readonly IApplicationPaths paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="BifService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="paths">Application paths utility.</param>
    public BifService(ILogger<BifService> logger, IApplicationPaths paths)
    {
        this.logger = logger;
        this.paths = paths;
    }

    /// <inheritdoc />
    public string GetBifPath(Guid metadataUuid, int partIndex)
    {
        var uuid = metadataUuid.ToString("N");
        if (uuid.Length < 2)
        {
            throw new ArgumentException("Invalid UUID for metadata.", nameof(metadataUuid));
        }

        var shard = uuid[..2];
        var baseDir = Path.Combine(this.paths.MediaDirectory, shard, uuid, "index");
        this.paths.EnsureDirectoryExists(baseDir);
        var bifFileName = partIndex == 0 ? "index.bif" : $"index-{partIndex}.bif";
        return Path.Combine(baseDir, bifFileName);
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        Guid metadataUuid,
        int partIndex,
        BifFile bifFile,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(bifFile);

        var bifPath = this.GetBifPath(metadataUuid, partIndex);

        // Sort entries by timestamp
        var sortedEntries = bifFile.Entries.OrderBy(e => e.TimestampMs).ToList();

        // Calculate offsets for each image
        var headerSize = BifHeaderSize + (sortedEntries.Count * BifEntrySize);
        var currentOffset = headerSize;
        var entriesWithOffsets = sortedEntries
            .Where(entry => bifFile.ImageData.ContainsKey(entry.TimestampMs))
            .Select(entry =>
            {
                var imageData = bifFile.ImageData[entry.TimestampMs];
                var result = (entry.TimestampMs, currentOffset, imageData);
                currentOffset += imageData.Length;
                return result;
            })
            .ToList();

        await using var fs = new FileStream(
            bifPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true
        );

        // Write header
        Span<byte> header = stackalloc byte[BifHeaderSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header[0..4], BifMagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..8], bifFile.Version);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..12], entriesWithOffsets.Count);
        BinaryPrimitives.WriteInt32LittleEndian(header[12..16], 1000); // Timestamp multiplier (ms)

        await fs.WriteAsync(header.ToArray(), cancellationToken).ConfigureAwait(false);

        // Write index entries
        var entryBuffer = new byte[BifEntrySize];
        foreach (var (timestamp, offset, _) in entriesWithOffsets)
        {
            BinaryPrimitives.WriteInt32LittleEndian(entryBuffer.AsSpan(0, 4), timestamp);
            BinaryPrimitives.WriteInt32LittleEndian(entryBuffer.AsSpan(4, 4), offset);
            await fs.WriteAsync(entryBuffer, cancellationToken).ConfigureAwait(false);
        }

        // Write image data
        foreach (var (_, _, imageData) in entriesWithOffsets)
        {
            await fs.WriteAsync(imageData, cancellationToken).ConfigureAwait(false);
        }

        await fs.FlushAsync(cancellationToken).ConfigureAwait(false);

        this.LogBifWritten(bifPath, entriesWithOffsets.Count);
    }

    /// <inheritdoc />
    public async Task<BifFile?> TryReadAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    )
    {
        var bifPath = this.GetBifPath(metadataUuid, partIndex);

        if (!File.Exists(bifPath))
        {
            return null;
        }

        try
        {
            await using var fs = new FileStream(
                bifPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            // Read header
            var header = new byte[BifHeaderSize];
            var bytesRead = await fs.ReadAsync(header, cancellationToken).ConfigureAwait(false);
            if (bytesRead < BifHeaderSize)
            {
                this.LogBifInvalidHeader(bifPath);
                return null;
            }

            var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
            if (magic != BifMagicNumber)
            {
                this.LogBifInvalidMagic(bifPath, magic);
                return null;
            }

            var version = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
            var frameCount = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));

            var bifFile = new BifFile { Version = version };

            // Read index entries
            var indexSize = frameCount * BifEntrySize;
            var indexData = new byte[indexSize];
            bytesRead = await fs.ReadAsync(indexData, cancellationToken).ConfigureAwait(false);
            if (bytesRead < indexSize)
            {
                this.LogBifInvalidIndex(bifPath);
                return null;
            }

            for (var i = 0; i < frameCount; i++)
            {
                var entryOffset = i * BifEntrySize;
                var timestamp = BinaryPrimitives.ReadInt32LittleEndian(
                    indexData.AsSpan(entryOffset, 4)
                );
                var offset = BinaryPrimitives.ReadInt32LittleEndian(
                    indexData.AsSpan(entryOffset + 4, 4)
                );
                bifFile.Entries.Add(new BifEntry(timestamp, offset));
            }

            // Read image data for each entry
            for (var i = 0; i < bifFile.Entries.Count; i++)
            {
                var entry = bifFile.Entries[i];
                var nextOffset =
                    i + 1 < bifFile.Entries.Count ? bifFile.Entries[i + 1].Offset : (int)fs.Length;
                var imageSize = nextOffset - entry.Offset;

                // Max 10MB per image
                if (imageSize <= 0 || imageSize > 10 * 1024 * 1024)
                {
                    this.LogBifInvalidImageSize(bifPath, entry.TimestampMs, imageSize);
                    continue;
                }

                fs.Seek(entry.Offset, SeekOrigin.Begin);
                var imageData = new byte[imageSize];
                bytesRead = await fs.ReadAsync(imageData, cancellationToken).ConfigureAwait(false);

                if (bytesRead == imageSize)
                {
                    bifFile.ImageData[entry.TimestampMs] = imageData;
                }
            }

            this.LogBifRead(bifPath, bifFile.Entries.Count);
            return bifFile;
        }
        catch (Exception ex)
        {
            this.LogBifReadFailed(bifPath, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<BifFile?> TryReadForPartAsync(
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

        // Determine part index within the media item
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
    public async Task<BifFile?> TryReadMetadataAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    )
    {
        var bifPath = this.GetBifPath(metadataUuid, partIndex);

        if (!File.Exists(bifPath))
        {
            return null;
        }

        try
        {
            await using var fs = new FileStream(
                bifPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            // Read header
            var header = new byte[BifHeaderSize];
            var bytesRead = await fs.ReadAsync(header, cancellationToken).ConfigureAwait(false);
            if (bytesRead < BifHeaderSize)
            {
                this.LogBifInvalidHeader(bifPath);
                return null;
            }

            var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
            if (magic != BifMagicNumber)
            {
                this.LogBifInvalidMagic(bifPath, magic);
                return null;
            }

            var version = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
            var frameCount = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));

            var bifFile = new BifFile { Version = version };

            // Read index entries only (no image data)
            var indexSize = frameCount * BifEntrySize;
            var indexData = new byte[indexSize];
            bytesRead = await fs.ReadAsync(indexData, cancellationToken).ConfigureAwait(false);
            if (bytesRead < indexSize)
            {
                this.LogBifInvalidIndex(bifPath);
                return null;
            }

            for (var i = 0; i < frameCount; i++)
            {
                var entryOffset = i * BifEntrySize;
                var timestamp = BinaryPrimitives.ReadInt32LittleEndian(
                    indexData.AsSpan(entryOffset, 4)
                );
                var offset = BinaryPrimitives.ReadInt32LittleEndian(
                    indexData.AsSpan(entryOffset + 4, 4)
                );
                bifFile.Entries.Add(new BifEntry(timestamp, offset));
            }

            this.LogBifMetadataRead(bifPath, bifFile.Entries.Count);
            return bifFile;
        }
        catch (Exception ex)
        {
            this.LogBifReadFailed(bifPath, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> TryReadThumbnailAsync(
        Guid metadataUuid,
        int partIndex,
        int thumbnailIndex,
        CancellationToken cancellationToken
    )
    {
        var bifPath = this.GetBifPath(metadataUuid, partIndex);

        if (!File.Exists(bifPath))
        {
            return null;
        }

        try
        {
            await using var fs = new FileStream(
                bifPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            // Read header
            var header = new byte[BifHeaderSize];
            var bytesRead = await fs.ReadAsync(header, cancellationToken).ConfigureAwait(false);
            if (bytesRead < BifHeaderSize)
            {
                return null;
            }

            var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
            if (magic != BifMagicNumber)
            {
                return null;
            }

            var frameCount = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));

            // Validate thumbnail index
            if (thumbnailIndex < 0 || thumbnailIndex >= frameCount)
            {
                return null;
            }

            // Read only the index entries we need (current and next)
            var indexOffset = BifHeaderSize + (thumbnailIndex * BifEntrySize);
            fs.Seek(indexOffset, SeekOrigin.Begin);

            var indexData = new byte[BifEntrySize * 2]; // Current + next entry
            var readSize = Math.Min(indexData.Length, (frameCount - thumbnailIndex) * BifEntrySize);
            bytesRead = await fs.ReadAsync(indexData.AsMemory(0, readSize), cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead < BifEntrySize)
            {
                return null;
            }

            var offset = BinaryPrimitives.ReadInt32LittleEndian(indexData.AsSpan(4, 4));

            // Determine image size
            int imageSize;
            if (thumbnailIndex + 1 < frameCount && bytesRead >= BifEntrySize * 2)
            {
                var nextOffset = BinaryPrimitives.ReadInt32LittleEndian(
                    indexData.AsSpan(BifEntrySize + 4, 4)
                );
                imageSize = nextOffset - offset;
            }
            else
            {
                // Last entry: calculate from file length
                imageSize = (int)fs.Length - offset;
            }

            // Validate image size (max 10MB)
            if (imageSize <= 0 || imageSize > 10 * 1024 * 1024)
            {
                return null;
            }

            // Seek to image data and read it
            fs.Seek(offset, SeekOrigin.Begin);
            var imageData = new byte[imageSize];
            bytesRead = await fs.ReadAsync(imageData, cancellationToken).ConfigureAwait(false);

            if (bytesRead == imageSize)
            {
                this.LogThumbnailRead(bifPath, thumbnailIndex, imageSize);
                return imageData;
            }

            return null;
        }
        catch (Exception ex)
        {
            this.LogBifReadFailed(bifPath, ex);
            return null;
        }
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Wrote BIF file: path={BifPath} frames={Count}"
    )]
    private partial void LogBifWritten(string bifPath, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Read BIF file: path={BifPath} frames={Count}"
    )]
    private partial void LogBifRead(string bifPath, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to read BIF file at {BifPath}")]
    private partial void LogBifReadFailed(string bifPath, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "BIF file has invalid header: {BifPath}")]
    private partial void LogBifInvalidHeader(string bifPath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "BIF file has invalid magic number: {BifPath} magic={Magic:X8}"
    )]
    private partial void LogBifInvalidMagic(string bifPath, uint magic);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "BIF file has invalid index data: {BifPath}"
    )]
    private partial void LogBifInvalidIndex(string bifPath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "BIF file has invalid image size for timestamp {TimestampMs}ms: {BifPath} size={Size}"
    )]
    private partial void LogBifInvalidImageSize(string bifPath, int timestampMs, int size);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Read BIF metadata (no images): path={BifPath} frames={Count}"
    )]
    private partial void LogBifMetadataRead(string bifPath, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Read thumbnail from BIF: path={BifPath} index={Index} size={Size} bytes"
    )]
    private partial void LogThumbnailRead(string bifPath, int index, int size);
    #endregion
}
