// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Manages real-time filesystem watching for library sections.
/// </summary>
/// <remarks>
/// Uses a hybrid approach: directories up to the configured depth are monitored
/// via native filesystem watchers, while deeper directories are polled periodically.
/// </remarks>
public interface ILibraryWatcherService
{
    /// <summary>
    /// Starts watching a library section's locations.
    /// </summary>
    /// <param name="librarySection">The library section to watch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartWatchingAsync(
        LibrarySection librarySection,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Stops watching a library section.
    /// </summary>
    /// <param name="librarySectionId">The ID of the library section to stop watching.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopWatchingAsync(int librarySectionId);

    /// <summary>
    /// Restarts watching for a library section with potentially updated settings.
    /// </summary>
    /// <param name="librarySection">The library section with updated settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RestartWatchingAsync(
        LibrarySection librarySection,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the current status of watchers for a library section.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <returns>The watcher status, or null if not watching.</returns>
    LibraryWatcherStatus? GetStatus(int librarySectionId);

    /// <summary>
    /// Gets the statuses of all active watchers.
    /// </summary>
    /// <returns>A dictionary mapping library section IDs to their watcher status.</returns>
    IReadOnlyDictionary<int, LibraryWatcherStatus> GetAllStatuses();
}

/// <summary>
/// Represents the status of a library section's filesystem watcher.
/// </summary>
public sealed class LibraryWatcherStatus
{
    /// <summary>
    /// Gets the library section ID.
    /// </summary>
    public required int LibrarySectionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the watcher is currently active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the number of directories being watched via native watchers.
    /// </summary>
    public required int WatchedDirectoryCount { get; init; }

    /// <summary>
    /// Gets the number of directories being monitored via polling.
    /// </summary>
    public required int PolledDirectoryCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last detected change.
    /// </summary>
    public DateTime? LastChangeDetected { get; init; }

    /// <summary>
    /// Gets the timestamp of the last polling run.
    /// </summary>
    public DateTime? LastPollingRun { get; init; }

    /// <summary>
    /// Gets the timestamp of the next scheduled polling run.
    /// </summary>
    public DateTime? NextPollingRun { get; init; }

    /// <summary>
    /// Gets a value indicating whether the watcher has encountered errors and needs a full rescan.
    /// </summary>
    public bool RequiresFullRescan { get; init; }

    /// <summary>
    /// Gets any error message from the watcher.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
