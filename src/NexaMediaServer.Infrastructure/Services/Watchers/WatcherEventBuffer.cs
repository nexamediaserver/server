// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Threading.Channels;
using Hangfire;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Buffers filesystem change events, debounces rapid changes, and coalesces related events
/// before dispatching them to micro-scan jobs.
/// </summary>
public sealed partial class WatcherEventBuffer : IDisposable
{
    /// <summary>
    /// Minimum debounce delay before processing events.
    /// </summary>
    public static readonly TimeSpan MinDebounceDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Maximum debounce delay - events will be processed after this time even if changes continue.
    /// </summary>
    public static readonly TimeSpan MaxDebounceDelay = TimeSpan.FromSeconds(2);

    private readonly ILogger<WatcherEventBuffer> logger;
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly ConcurrentDictionary<int, LibraryEventBuffer> libraryBuffers = new();
    private readonly ConcurrentDictionary<int, bool> librariesRequiringFullRescan = new();
    private readonly Channel<FileSystemChangeEvent> incomingEvents;
    private readonly CancellationTokenSource cts = new();
    private readonly Task processingTask;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatcherEventBuffer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="backgroundJobClient">The Hangfire background job client.</param>
    public WatcherEventBuffer(
        ILogger<WatcherEventBuffer> logger,
        IBackgroundJobClient backgroundJobClient
    )
    {
        this.logger = logger;
        this.backgroundJobClient = backgroundJobClient;

        // Bounded channel to prevent memory issues if events flood in
        this.incomingEvents = Channel.CreateBounded<FileSystemChangeEvent>(
            new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            }
        );

        this.processingTask = Task.Run(this.ProcessEventsAsync);
    }

    /// <summary>
    /// Enqueues a filesystem change event for processing.
    /// </summary>
    /// <param name="evt">The filesystem change event.</param>
    public void Enqueue(FileSystemChangeEvent evt)
    {
        if (this.disposed)
        {
            return;
        }

        // Try to write to the channel; if full, oldest events are dropped
        if (!this.incomingEvents.Writer.TryWrite(evt))
        {
            LogEventDropped(this.logger, evt.Path, evt.LibrarySectionId);
        }
    }

    /// <summary>
    /// Marks a library as requiring a full rescan due to watcher errors.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    public void MarkLibraryForFullRescan(int librarySectionId)
    {
        this.librariesRequiringFullRescan[librarySectionId] = true;

        // Clear any pending events for this library since we need a full rescan
        if (this.libraryBuffers.TryRemove(librarySectionId, out var buffer))
        {
            buffer.Clear();
        }

        // Dispatch full rescan job
        this.DispatchFullRescanJob(librarySectionId);
    }

    /// <summary>
    /// Disposes the event buffer and stops processing.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.incomingEvents.Writer.Complete();
        this.cts.Cancel();

        try
        {
            this.processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Expected when cancellation occurs
        }

        this.cts.Dispose();

        foreach (var buffer in this.libraryBuffers.Values)
        {
            buffer.Dispose();
        }

        this.libraryBuffers.Clear();
    }

    /// <summary>
    /// Flushes the buffer for a specific library and dispatches a micro-scan job.
    /// Called by LibraryEventBuffer when debounce completes.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="coalescedEvent">The coalesced change event to process.</param>
    internal void FlushLibraryBuffer(int librarySectionId, CoalescedChangeEvent coalescedEvent)
    {
        if (this.disposed)
        {
            return;
        }

        // Dispatch micro-scan job via Hangfire
        try
        {
            if (coalescedEvent.PathsToScan.Count > 0 || coalescedEvent.PathsToRemove.Count > 0)
            {
                this.backgroundJobClient.Enqueue<IMicroScanJob>(job =>
                    job.ExecuteAsync(coalescedEvent, CancellationToken.None)
                );

                LogMicroScanDispatched(
                    this.logger,
                    librarySectionId,
                    coalescedEvent.PathsToScan.Count,
                    coalescedEvent.PathsToRemove.Count
                );
            }
        }
        catch (Exception ex)
        {
            LogDispatchError(this.logger, librarySectionId, ex);
        }
    }

    private async Task ProcessEventsAsync()
    {
        try
        {
            await foreach (var evt in this.incomingEvents.Reader.ReadAllAsync(this.cts.Token))
            {
                // Skip if library needs full rescan
                if (this.librariesRequiringFullRescan.ContainsKey(evt.LibrarySectionId))
                {
                    continue;
                }

                var buffer = this.libraryBuffers.GetOrAdd(
                    evt.LibrarySectionId,
                    id => new LibraryEventBuffer(id, this)
                );

                buffer.Add(evt);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            LogProcessingError(this.logger, ex);
        }
    }

    private void DispatchFullRescanJob(int librarySectionId)
    {
        try
        {
            this.backgroundJobClient.Enqueue<ILibraryScannerService>(service =>
                service.StartScanAsync(librarySectionId)
            );

            LogFullRescanDispatched(this.logger, librarySectionId);
        }
        catch (Exception ex)
        {
            LogDispatchError(this.logger, librarySectionId, ex);
        }
    }
}
