// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for orchestrating collection image generation.
/// </summary>
/// <remarks>
/// This service is responsible for generating collage thumbnails for collection-type metadata items
/// (e.g., PhotoAlbum, PictureSet, BookSeries, GameSeries, Collection, Playlist).
/// It runs as a separate process after all metadata refreshes for a scan are complete.
/// </remarks>
public interface ICollectionImageOrchestrator
{
    /// <summary>
    /// Generates collection images for all collection-type metadata items in a library.
    /// </summary>
    /// <remarks>
    /// This method should be called after all metadata refresh jobs for a library scan have completed.
    /// It queries all collection-type items that have children with thumbnails and generates
    /// a 2x2 collage thumbnail for each.
    /// </remarks>
    /// <param name="libraryId">The ID of the library to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("image_generators")]
    Task GenerateCollectionImagesForLibraryAsync(int libraryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates the collection image for a specific parent metadata item.
    /// </summary>
    /// <remarks>
    /// This method is typically called when a child item's thumbnail is updated,
    /// triggering a refresh of the parent collection's collage thumbnail.
    /// Uses a 5-minute debounce window to avoid excessive regeneration during bulk updates.
    /// </remarks>
    /// <param name="parentUuid">The UUID of the parent collection metadata item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("image_generators")]
    Task RegenerateCollectionImageAsync(Guid parentUuid, CancellationToken cancellationToken = default);
}
