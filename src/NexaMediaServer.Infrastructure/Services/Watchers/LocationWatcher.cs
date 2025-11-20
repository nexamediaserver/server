// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using IODirectory = System.IO.Directory;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Watches a single location (directory) using FileSystemWatcher for shallow levels
/// and polling for deep directories.
/// </summary>
internal sealed partial class LocationWatcher : IDisposable
{
    private readonly int librarySectionId;
    private readonly SectionLocation location;
    private readonly int maxWatchDepth;
    private readonly Action<FileSystemChangeEvent> onEvent;
    private readonly Action<int, Exception> onError;
    private readonly ILogger logger;
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly ConcurrentDictionary<string, DateTime> deepDirectoryTimestamps = new();
    private readonly object syncLock = new();
    private bool isRunning;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationWatcher"/> class.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="location">The section location to watch.</param>
    /// <param name="maxWatchDepth">Maximum depth for native FileSystemWatcher.</param>
    /// <param name="onEvent">Callback for filesystem events.</param>
    /// <param name="onError">Callback for watcher errors.</param>
    /// <param name="logger">The logger instance.</param>
    public LocationWatcher(
        int librarySectionId,
        SectionLocation location,
        int maxWatchDepth,
        Action<FileSystemChangeEvent> onEvent,
        Action<int, Exception> onError,
        ILogger logger
    )
    {
        this.librarySectionId = librarySectionId;
        this.location = location;
        this.maxWatchDepth = maxWatchDepth;
        this.onEvent = onEvent;
        this.onError = onError;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the number of directories being watched by native FileSystemWatcher.
    /// </summary>
    public int WatchedCount => this.watchers.Count;

    /// <summary>
    /// Gets the number of directories being monitored via polling.
    /// </summary>
    public int PolledCount => this.deepDirectoryTimestamps.Count;

    /// <summary>
    /// Starts watching the location.
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
            this.CreateWatchers(this.location.RootPath, 0);
            LogWatcherStarted(this.logger, this.location.RootPath, this.watchers.Count);
        }
    }

    /// <summary>
    /// Stops watching the location.
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

            foreach (var watcher in this.watchers)
            {
                watcher.EnableRaisingEvents = false;
            }
        }
    }

    /// <summary>
    /// Polls deep directories that aren't covered by FileSystemWatcher.
    /// </summary>
    public void PollDeepDirectories()
    {
        if (!this.isRunning || this.disposed)
        {
            return;
        }

        // Find directories beyond our watch depth and check for changes
        try
        {
            this.PollDirectory(this.location.RootPath, 0);
        }
        catch (Exception ex)
        {
            LogPollingError(this.logger, this.location.RootPath, ex);
        }
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

        foreach (var watcher in this.watchers)
        {
            watcher.Dispose();
        }

        this.watchers.Clear();
    }

    private void CreateWatchers(string path, int depth)
    {
        if (depth >= this.maxWatchDepth)
        {
            // Beyond watch depth - track for polling
            this.deepDirectoryTimestamps[path] = IODirectory.GetLastWriteTimeUtc(path);
            return;
        }

        try
        {
            // Create watcher for this directory (non-recursive)
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter =
                    NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
                    | NotifyFilters.CreationTime,
                IncludeSubdirectories = false, // We manage depth ourselves
                EnableRaisingEvents = false, // Enable after setup
            };

            watcher.Created += this.OnCreated;
            watcher.Deleted += this.OnDeleted;
            watcher.Changed += this.OnChanged;
            watcher.Renamed += this.OnRenamed;
            watcher.Error += this.OnError;

            this.watchers.Add(watcher);
            watcher.EnableRaisingEvents = true;

            // Recursively create watchers for subdirectories up to depth
            foreach (var subDir in IODirectory.EnumerateDirectories(path))
            {
                this.CreateWatchers(subDir, depth + 1);
            }
        }
        catch (Exception ex)
        {
            LogWatcherCreateError(this.logger, path, ex);
        }
    }

    private void PollDirectory(string path, int depth)
    {
        // Only poll directories beyond our watch depth
        if (depth < this.maxWatchDepth)
        {
            foreach (var subDir in IODirectory.EnumerateDirectories(path))
            {
                this.PollDirectory(subDir, depth + 1);
            }

            return;
        }

        // Check if this deep directory has changed
        var currentTime = IODirectory.GetLastWriteTimeUtc(path);

        if (
            this.deepDirectoryTimestamps.TryGetValue(path, out var lastTime)
            && currentTime > lastTime
        )
        {
            // Directory modified - emit event
            this.EmitEvent(path, FileSystemChangeType.Modified, isDirectory: true);
        }

        this.deepDirectoryTimestamps[path] = currentTime;

        // Recursively poll subdirectories
        foreach (var subDir in IODirectory.EnumerateDirectories(path))
        {
            this.PollDirectory(subDir, depth + 1);
        }
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        var isDirectory = IODirectory.Exists(e.FullPath);
        this.EmitEvent(e.FullPath, FileSystemChangeType.Created, isDirectory);

        // If a new directory was created within our watch depth, add a watcher for it
        if (isDirectory)
        {
            var depth = this.GetDepth(e.FullPath);
            if (depth < this.maxWatchDepth)
            {
                lock (this.syncLock)
                {
                    this.CreateWatchers(e.FullPath, depth);
                }
            }
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        // We can't check if it was a directory after deletion, assume file
        // The coalescing logic will handle this appropriately
        this.EmitEvent(e.FullPath, FileSystemChangeType.Deleted, isDirectory: false);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var isDirectory = IODirectory.Exists(e.FullPath);
        this.EmitEvent(e.FullPath, FileSystemChangeType.Modified, isDirectory);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var isDirectory = IODirectory.Exists(e.FullPath);
        this.onEvent(
            new FileSystemChangeEvent
            {
                LibrarySectionId = this.librarySectionId,
                SectionLocationId = this.location.Id,
                Path = e.FullPath,
                OldPath = e.OldFullPath,
                ChangeType = FileSystemChangeType.Renamed,
                IsDirectory = isDirectory,
                DetectedAt = DateTime.UtcNow,
            }
        );
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        this.onError(this.location.Id, e.GetException());
    }

    private void EmitEvent(string path, FileSystemChangeType changeType, bool isDirectory)
    {
        this.onEvent(
            new FileSystemChangeEvent
            {
                LibrarySectionId = this.librarySectionId,
                SectionLocationId = this.location.Id,
                Path = path,
                ChangeType = changeType,
                IsDirectory = isDirectory,
                DetectedAt = DateTime.UtcNow,
            }
        );
    }

    private int GetDepth(string path)
    {
        var relativePath = Path.GetRelativePath(this.location.RootPath, path);
        return relativePath.Split(Path.DirectorySeparatorChar).Length - 1;
    }
}
