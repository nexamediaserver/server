// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Buffers and coalesces filesystem events for a single library section.
/// Debounces rapid changes and produces coalesced events for efficient processing.
/// </summary>
internal sealed class LibraryEventBuffer : IDisposable
{
    private readonly int librarySectionId;
    private readonly WatcherEventBuffer parentBuffer;
    private readonly ConcurrentDictionary<string, PathEventState> pathStates = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Timer debounceTimer;
    private readonly object syncLock = new();
    private DateTime firstEventTime = DateTime.MaxValue;
    private DateTime lastEventTime = DateTime.MinValue;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryEventBuffer"/> class.
    /// </summary>
    /// <param name="librarySectionId">The library section ID this buffer is for.</param>
    /// <param name="parentBuffer">The parent watcher event buffer.</param>
    public LibraryEventBuffer(int librarySectionId, WatcherEventBuffer parentBuffer)
    {
        this.librarySectionId = librarySectionId;
        this.parentBuffer = parentBuffer;
        this.debounceTimer = new Timer(
            this.OnDebounceTimerElapsed,
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );
    }

    /// <summary>
    /// Adds a filesystem change event to the buffer.
    /// </summary>
    /// <param name="evt">The filesystem change event to add.</param>
    public void Add(FileSystemChangeEvent evt)
    {
        if (this.disposed)
        {
            return;
        }

        lock (this.syncLock)
        {
            var now = DateTime.UtcNow;

            // Track timing for debounce logic
            if (this.firstEventTime == DateTime.MaxValue)
            {
                this.firstEventTime = now;
            }

            this.lastEventTime = now;

            // Update or add path state
            this.pathStates.AddOrUpdate(
                evt.Path,
                _ => new PathEventState(evt),
                (_, existing) => existing.Merge(evt)
            );

            // Handle renames - also track old path for removal
            if (
                evt.ChangeType == FileSystemChangeType.Renamed
                && !string.IsNullOrEmpty(evt.OldPath)
            )
            {
                this.pathStates.AddOrUpdate(
                    evt.OldPath,
                    _ => new PathEventState(
                        new FileSystemChangeEvent
                        {
                            LibrarySectionId = evt.LibrarySectionId,
                            SectionLocationId = evt.SectionLocationId,
                            Path = evt.OldPath,
                            ChangeType = FileSystemChangeType.Deleted,
                            IsDirectory = evt.IsDirectory,
                            DetectedAt = evt.DetectedAt,
                        }
                    ),
                    (_, existing) => existing.MarkDeleted()
                );
            }

            // Reset debounce timer
            var window = this.parentBuffer.GetDebounceWindow(this.librarySectionId);
            this.debounceTimer.Change(window.Min, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Clears all pending events in the buffer.
    /// </summary>
    public void Clear()
    {
        lock (this.syncLock)
        {
            this.pathStates.Clear();
            this.firstEventTime = DateTime.MaxValue;
            this.lastEventTime = DateTime.MinValue;
            this.debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Disposes the buffer and stops timers.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.debounceTimer.Dispose();
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        if (this.disposed)
        {
            return;
        }

        lock (this.syncLock)
        {
            var now = DateTime.UtcNow;

            // Check if we should wait more (max delay not reached and recent activity)
            var timeSinceFirst = now - this.firstEventTime;
            var timeSinceLast = now - this.lastEventTime;
            var window = this.parentBuffer.GetDebounceWindow(this.librarySectionId);

            if (timeSinceFirst < window.Max && timeSinceLast < window.Min)
            {
                // Recent activity, reset timer
                this.debounceTimer.Change(window.Min, Timeout.InfiniteTimeSpan);
                return;
            }

            // Time to flush - collect events
            var pathsToScan = new List<string>();
            var pathsToRemove = new List<string>();

            foreach (var kvp in this.pathStates)
            {
                switch (kvp.Value.FinalAction)
                {
                    case PathFinalAction.Scan:
                        pathsToScan.Add(kvp.Key);
                        break;
                    case PathFinalAction.Remove:
                        pathsToRemove.Add(kvp.Key);
                        break;
                    case PathFinalAction.Ignore:
                    default:
                        // Created then deleted - no action needed
                        break;
                }
            }

            // Clear buffer state
            this.pathStates.Clear();
            this.firstEventTime = DateTime.MaxValue;
            this.lastEventTime = DateTime.MinValue;

            if (pathsToScan.Count == 0 && pathsToRemove.Count == 0)
            {
                return;
            }

            // Create coalesced event and dispatch
            var coalescedEvent = new CoalescedChangeEvent
            {
                LibrarySectionId = this.librarySectionId,
                PathsToScan = pathsToScan,
                PathsToRemove = pathsToRemove,
                CreatedAt = DateTime.UtcNow,
            };

            this.parentBuffer.FlushLibraryBuffer(this.librarySectionId, coalescedEvent);
        }
    }
}
