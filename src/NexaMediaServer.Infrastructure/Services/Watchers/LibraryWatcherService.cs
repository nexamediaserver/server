// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Service that manages filesystem watchers for library sections.
/// Implements a hybrid approach: native FileSystemWatcher for top N levels,
/// with periodic polling for deeper directories.
/// </summary>
public sealed partial class LibraryWatcherService : ILibraryWatcherService, IDisposable
{
    private readonly ILogger<LibraryWatcherService> logger;
    private readonly WatcherEventBuffer eventBuffer;
    private readonly ConcurrentDictionary<int, LibrarySectionWatcher> libraryWatchers = new();
    private readonly SemaphoreSlim startStopLock = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryWatcherService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="eventBuffer">The event buffer for coalescing filesystem events.</param>
    public LibraryWatcherService(
        ILogger<LibraryWatcherService> logger,
        WatcherEventBuffer eventBuffer
    )
    {
        this.logger = logger;
        this.eventBuffer = eventBuffer;
    }

    /// <inheritdoc/>
    public async Task StartWatchingAsync(
        LibrarySection librarySection,
        CancellationToken cancellationToken = default
    )
    {
        await this.startStopLock.WaitAsync(cancellationToken);
        try
        {
            if (this.libraryWatchers.ContainsKey(librarySection.Id))
            {
                LogAlreadyWatching(this.logger, librarySection.Id);
                return;
            }

            var settings = librarySection.Settings;
            if (settings?.WatcherEnabled != true)
            {
                LogWatcherDisabled(this.logger, librarySection.Id);
                return;
            }

            var watcher = new LibrarySectionWatcher(
                librarySection,
                settings,
                this.eventBuffer,
                this.logger
            );

            if (this.libraryWatchers.TryAdd(librarySection.Id, watcher))
            {
                watcher.Start();
                LogWatcherStarted(this.logger, librarySection.Id, librarySection.Locations.Count);
            }
        }
        finally
        {
            this.startStopLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task StopWatchingAsync(int librarySectionId)
    {
        await this.startStopLock.WaitAsync();
        try
        {
            if (this.libraryWatchers.TryRemove(librarySectionId, out var watcher))
            {
                watcher.Stop();
                watcher.Dispose();
                LogWatcherStopped(this.logger, librarySectionId);
            }
        }
        finally
        {
            this.startStopLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RestartWatchingAsync(
        LibrarySection librarySection,
        CancellationToken cancellationToken = default
    )
    {
        await this.StopWatchingAsync(librarySection.Id);
        await this.StartWatchingAsync(librarySection, cancellationToken);
    }

    /// <inheritdoc/>
    public LibraryWatcherStatus? GetStatus(int librarySectionId)
    {
        if (!this.libraryWatchers.TryGetValue(librarySectionId, out var watcher))
        {
            return null;
        }

        return watcher.GetStatus();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<int, LibraryWatcherStatus> GetAllStatuses()
    {
        var result = new Dictionary<int, LibraryWatcherStatus>();

        foreach (var kvp in this.libraryWatchers)
        {
            result[kvp.Key] = kvp.Value.GetStatus();
        }

        return result;
    }

    /// <summary>
    /// Disposes all watchers and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        foreach (var watcher in this.libraryWatchers.Values)
        {
            watcher.Stop();
            watcher.Dispose();
        }

        this.libraryWatchers.Clear();
        this.startStopLock.Dispose();
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Library {LibraryId} is already being watched"
    )]
    private static partial void LogAlreadyWatching(ILogger logger, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Watcher disabled for library {LibraryId}"
    )]
    private static partial void LogWatcherDisabled(ILogger logger, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Started watcher for library {LibraryId} with {LocationCount} locations"
    )]
    private static partial void LogWatcherStarted(ILogger logger, int libraryId, int locationCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stopped watcher for library {LibraryId}"
    )]
    private static partial void LogWatcherStopped(ILogger logger, int libraryId);
}
