// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides utilities to build, read and write GoP (Group of Pictures) XML indexes.
/// </summary>
public interface IGopIndexService
{
    /// <summary>
    /// Gets the canonical file path for a GoP XML for a given metadata UUID and part index.
    /// </summary>
    /// <param name="metadata">The metadata item owning the media.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <returns>The full file path where the GoP XML should reside.</returns>
    string GetGopPath(MetadataItem metadata, int partIndex);

    /// <summary>
    /// Writes a GoP XML file for the supplied groups (atomic write).
    /// </summary>
    /// <param name="metadata">The owning metadata item (used for path sharding).</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="index">The GoP index to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(
        MetadataItem metadata,
        int partIndex,
        GopIndex index,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Attempts to read and parse a GoP XML file.
    /// </summary>
    /// <param name="metadata">The owning metadata item.</param>
    /// <param name="partIndex">The zero-based part index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed index, or null if not found or invalid.</returns>
    /// <returns>The parsed index or null.</returns>
    Task<GopIndex?> TryReadAsync(
        MetadataItem metadata,
        int partIndex,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Convenience helper to locate and read a GoP index for the specific media part.
    /// </summary>
    /// <param name="mediaItem">The owning media item.</param>
    /// <param name="mediaPart">The specific media part.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed index, or null if unavailable.</returns>
    /// <returns>The parsed index or null.</returns>
    Task<GopIndex?> TryReadForPartAsync(
        MediaItem mediaItem,
        MediaPart mediaPart,
        CancellationToken cancellationToken
    );
}
