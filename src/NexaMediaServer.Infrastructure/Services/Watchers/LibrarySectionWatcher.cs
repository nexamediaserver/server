// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Watches a single library section using a hybrid approach:
/// native FileSystemWatcher for shallow directories, polling for deep ones.
/// </summary>
internal sealed partial class LibrarySectionWatcher : IDisposable
{
    private readonly LibrarySection library;
    private readonly LibrarySectionSetting settings;
    private readonly WatcherEventBuffer eventBuffer;
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<int, LocationWatcher> locationWatchers = new();
    private readonly Timer? pollingTimer;
    private readonly object syncLock = new();
    private DateTime lastPollingRun = DateTime.MinValue;
    private bool isRunning;
    private bool disposed;
    private int errorCount;
    private DateTime? lastErrorTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrarySectionWatcher"/> class.
    /// </summary>
    /// <param name="library">The library section to watch.</param>
    /// <param name="settings">The watcher settings.</param>
    /// <param name="eventBuffer">The event buffer for coalescing events.</param>
    /// <param name="logger">The logger instance.</param>
    public LibrarySectionWatcher(
        LibrarySection library,
        LibrarySectionSetting settings,
        WatcherEventBuffer eventBuffer,
        ILogger logger
    )
    {
        this.library = library;
        this.settings = settings;
        this.eventBuffer = eventBuffer;
        this.logger = logger;

        // Create polling timer but don't start it yet
        this.pollingTimer = new Timer(
            this.OnPollingTimerElapsed,
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );
    }

    /// <summary>
    /// Starts watching all locations in the library section.
    /// </summary>
    public void Start()
    {
        lock (this.syncLock)
        {
            if (this.isRunning || this.disposed)
            {
                return;
            }

            this.isRunning = true;

            foreach (var location in this.library.Locations)
            {
                this.StartLocationWatcher(location);
            }

            // Start polling timer for deep directories
            var pollingInterval = this.settings.WatcherPollingInterval;
            this.pollingTimer?.Change(pollingInterval, pollingInterval);
        }
    }

    /// <summary>
    /// Stops watching all locations.
    /// </summary>
    public void Stop()
    {
        lock (this.syncLock)
        {
            if (!this.isRunning)
            {
                return;
            }

            this.isRunning = false;
            this.pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            foreach (var watcher in this.locationWatchers.Values)
            {
                watcher.Stop();
            }
        }
    }

    /// <summary>
    /// Gets the current status of this watcher.
    /// </summary>
    /// <returns>The watcher status.</returns>
    public LibraryWatcherStatus GetStatus()
    {
        var pollingInterval = this.settings.WatcherPollingInterval;

        return new LibraryWatcherStatus
        {
            LibrarySectionId = this.library.Id,
            IsActive = this.isRunning,
            WatchedDirectoryCount = this.locationWatchers.Sum(w => w.Value.WatchedCount),
            PolledDirectoryCount = this.locationWatchers.Sum(w => w.Value.PolledCount),
            LastPollingRun = this.lastPollingRun == DateTime.MinValue ? null : this.lastPollingRun,
            NextPollingRun = this.isRunning ? this.lastPollingRun + pollingInterval : null,
            RequiresFullRescan = this.errorCount > 10,
            ErrorMessage = this.lastErrorTime.HasValue
                ? $"Encountered {this.errorCount} errors, last at {this.lastErrorTime}"
                : null,
        };
    }

    /// <summary>
    /// Disposes all resources.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.Stop();
        this.pollingTimer?.Dispose();

        foreach (var watcher in this.locationWatchers.Values)
        {
            watcher.Dispose();
        }

        this.locationWatchers.Clear();
    }

    private void StartLocationWatcher(SectionLocation location)
    {
        if (!System.IO.Directory.Exists(location.RootPath))
        {
            LogLocationNotFound(this.logger, location.RootPath);
            return;
        }

        var depth = this.settings.WatcherDepth;
        var watcher = new LocationWatcher(
            this.library.Id,
            location,
            depth,
            this.OnFileSystemEvent,
            this.OnWatcherError,
            this.logger
        );

        if (this.locationWatchers.TryAdd(location.Id, watcher))
        {
            watcher.Start();
        }
    }

    private void OnFileSystemEvent(FileSystemChangeEvent evt)
    {
        if (!this.isRunning)
        {
            return;
        }

        this.eventBuffer.Enqueue(evt);
    }

    private void OnWatcherError(int locationId, Exception error)
    {
        this.errorCount++;
        this.lastErrorTime = DateTime.UtcNow;

        LogWatcherError(this.logger, this.library.Id, locationId, error);

        // If too many errors, mark library for full rescan
        if (this.errorCount > 10)
        {
            LogTooManyErrors(this.logger, this.library.Id);
            this.eventBuffer.MarkLibraryForFullRescan(this.library.Id);
        }
    }

    private void OnPollingTimerElapsed(object? state)
    {
        if (!this.isRunning || this.disposed)
        {
            return;
        }

        this.lastPollingRun = DateTime.UtcNow;

        // Poll deep directories that aren't covered by FileSystemWatcher
        foreach (var watcher in this.locationWatchers.Values)
        {
            try
            {
                watcher.PollDeepDirectories();
            }
            catch (Exception ex)
            {
                LogPollingError(this.logger, this.library.Id, ex);
            }
        }
    }
}
