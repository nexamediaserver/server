// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository interface for accessing media parts.
/// </summary>
public interface IMediaPartRepository
{
    /// <summary>
    /// Get queryable for media parts.
    /// </summary>
    /// <returns>An IQueryable of MediaPart entities.</returns>
    IQueryable<MediaPart> GetQueryable();

    /// <summary>
    /// Get a media part by id.
    /// </summary>
    /// <param name="id">The media part identifier.</param>
    /// <returns>The media part if found; otherwise null.</returns>
    Task<MediaPart?> GetByIdAsync(int id);

    /// <summary>
    /// Delete a media part by file path.
    /// </summary>
    /// <param name="part">The file path of the media part to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteByFilePathAsync(string part);

    /// <summary>
    /// Deletes media parts by a collection of file paths using chunked/bulk operations.
    /// </summary>
    /// <param name="filePaths">The file paths to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteByFilePathsAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Streams all media part file paths for a given library without loading them all into memory.
    /// </summary>
    /// <param name="libraryId">The library identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async stream of file paths.</returns>
    IAsyncEnumerable<string> StreamFilePathsByLibraryIdAsync(
        int libraryId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get all media part file paths for a given library, projected as strings with no tracking.
    /// </summary>
    /// <param name="libraryId">The library identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A hash set of file paths (case-insensitive comparer).</returns>
    Task<HashSet<string>> GetFilePathsByLibraryIdAsync(
        int libraryId,
        CancellationToken cancellationToken = default
    );
}
