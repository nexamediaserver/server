// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Provides image generation or extraction (thumbnails, posters) for a <see cref="MediaItem"/> associated with a typed metadata item.
/// Results are not persisted yet; the pipeline executes for side-effects or caching.
/// </summary>
/// <typeparam name="TMetadata">The metadata DTO type handled by this provider.</typeparam>
public interface IImageProvider<in TMetadata>
    where TMetadata : MetadataBaseItem
{
    /// <summary>
    /// Gets the unique name of the image provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the ordering priority. Lower values run first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Determines if this provider supports the given item.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <param name="metadata">The parent metadata item (tracked) for contextual image decisions.</param>
    /// <returns>True if supported, otherwise false.</returns>
    bool Supports(MediaItem item, TMetadata metadata);

    /// <summary>
    /// Runs the provider to generate or extract images for the given media item.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <param name="metadata">The parent metadata item (tracked) for contextual image decisions.</param>
    /// <param name="parts">The associated media parts (may be empty).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProvideAsync(
        MediaItem item,
        TMetadata metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    );
}
