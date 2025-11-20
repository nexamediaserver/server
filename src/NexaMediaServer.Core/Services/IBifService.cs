// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides utilities to build, read and write BIF (Base Index Frames) files for video trickplay.
/// </summary>
public interface IBifService
{
    /// <summary>
    /// Gets the canonical file path for a BIF file for a given metadata item and part index.
    /// </summary>
    /// <param name="metadataUuid">The metadata item's UUID.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <returns>The full file path where the BIF file should reside.</returns>
    string GetBifPath(Guid metadataUuid, int partIndex);

    /// <summary>
    /// Writes a BIF file for the supplied frame entries and image data.
    /// </summary>
    /// <param name="metadataUuid">The owning metadata item's UUID (used for path sharding).</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="bifFile">The BIF file data to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(
        Guid metadataUuid,
        int partIndex,
        BifFile bifFile,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Attempts to read and parse a BIF file.
    /// </summary>
    /// <param name="metadataUuid">The owning metadata item's UUID.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed BIF file, or null if not found or invalid.</returns>
    Task<BifFile?> TryReadAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Convenience helper to locate and read a BIF file for the specific media part.
    /// </summary>
    /// <param name="mediaItem">The owning media item.</param>
    /// <param name="mediaPart">The specific media part.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed BIF file, or null if unavailable.</returns>
    Task<BifFile?> TryReadForPartAsync(
        MediaItem mediaItem,
        MediaPart mediaPart,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads only the BIF file metadata (header and index entries) without loading image data.
    /// This is much faster and uses less memory than reading the full file.
    /// </summary>
    /// <param name="metadataUuid">The owning metadata item's UUID.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The BIF file with entries populated but no image data, or null if not found.</returns>
    Task<BifFile?> TryReadMetadataAsync(
        Guid metadataUuid,
        int partIndex,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads a single thumbnail image from a BIF file at the specified index.
    /// This is much more efficient than loading the entire BIF file.
    /// </summary>
    /// <param name="metadataUuid">The owning metadata item's UUID.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="thumbnailIndex">The thumbnail index (0-based) to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JPEG image bytes, or null if not found or index out of range.</returns>
    Task<byte[]?> TryReadThumbnailAsync(
        Guid metadataUuid,
        int partIndex,
        int thumbnailIndex,
        CancellationToken cancellationToken
    );
}
