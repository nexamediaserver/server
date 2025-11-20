// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service for scanning media libraries.
/// </summary>
public interface ILibraryScannerService
{
    /// <summary>
    /// Starts a scan of the specified library.
    /// </summary>
    /// <param name="libraryId">The ID of the library to scan.</param>
    /// <returns>The ID of the scan operation.</returns>
    Task<int> StartScanAsync(int libraryId);

    /// <summary>
    /// Gets the status of a scan operation.
    /// </summary>
    /// <param name="scanId">The ID of the scan operation.</param>
    /// <returns>The scan status, or null if the scan does not exist.</returns>
    Task<LibraryScan?> GetScanStatusAsync(int scanId);

    /// <summary>
    /// Cancels a scan operation.
    /// </summary>
    /// <param name="scanId">The ID of the scan operation to cancel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CancelScanAsync(int scanId);

    /// <summary>
    /// Gets the scan history for a library.
    /// </summary>
    /// <param name="libraryId">The ID of the library.</param>
    /// <returns>A queryable collection of scan records.</returns>
    IQueryable<LibraryScan> GetScanHistoryQueryable(int libraryId);

    /// <summary>
    /// Gets all scans that were interrupted (in Running status with a checkpoint).
    /// </summary>
    /// <returns>A list of interrupted scans.</returns>
    Task<IReadOnlyList<LibraryScan>> GetInterruptedScansAsync();

    /// <summary>
    /// Resumes an interrupted scan from its checkpoint.
    /// </summary>
    /// <param name="scanId">The ID of the interrupted scan.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeScanAsync(int scanId);

    /// <summary>
    /// Scans a specific path within a library for changes.
    /// Used for targeted micro-scans triggered by filesystem watchers.
    /// </summary>
    /// <param name="libraryId">The ID of the library containing the path.</param>
    /// <param name="path">The absolute path to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScanPathAsync(int libraryId, string path, CancellationToken cancellationToken = default);
}
