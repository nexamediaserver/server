// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides functionality for scanning directories and identifying media files.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Streams a directory tree, yielding one batch of files per directory as soon as each directory is scanned.
    /// This enables incremental, non-blocking processing without waiting for the entire tree.
    /// </summary>
    /// <param name="path">The root directory path to scan recursively.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async stream of batches, one per directory scanned.</returns>
    IAsyncEnumerable<ScannedDirectoryBatch> ScanDirectoryStreamingAsync(
        string path,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Represents the results of scanning a single directory.
/// </summary>
/// <param name="DirectoryPath">The directory that was scanned.</param>
/// <param name="Files">The files found in that directory.</param>
public readonly record struct ScannedDirectoryBatch(
    string DirectoryPath,
    IReadOnlyList<FileSystemMetadata> Files
);
