// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Ardalis.GuardClauses;
using Hangfire;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides functionality for scanning media libraries and managing scan operations.
/// </summary>
public partial class LibraryScannerService : ILibraryScannerService
{
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> RunningScans = new();

    private readonly ILibrarySectionRepository libraryRepository;
    private readonly ILibraryScanRepository scanRepository;
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly IMediaPartRepository mediaPartRepository;
    private readonly IDirectoryRepository directoryRepository;
    private readonly IFileScanner fileScanner;
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<LibraryScannerService> logger;
    private readonly IEnumerable<IItemResolver> itemResolvers;
    private readonly IBackgroundJobClient metadataJobClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryScannerService"/> class.
    /// </summary>
    /// <param name="libraryRepository">The library repository for data access.</param>
    /// <param name="scanRepository">The library scan repository for scan data access.</param>
    /// <param name="metadataItemRepository">The metadata item repository for metadata access.</param>
    /// <param name="mediaPartRepository">The media part repository for media part access.</param>
    /// <param name="fileScanner">The file scanner for scanning media files.</param>
    /// <param name="jobClient">The background job client for scheduling tasks.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="itemResolvers">The ordered set of item resolvers to classify scanned files.</param>
    /// <param name="directoryRepository">The directory repository for persisting directory graph.</param>
    /// <param name="metadataJobClient">The Hangfire client for queuing metadata jobs.</param>
    public LibraryScannerService(
        ILibrarySectionRepository libraryRepository,
        ILibraryScanRepository scanRepository,
        IMetadataItemRepository metadataItemRepository,
        IMediaPartRepository mediaPartRepository,
        IDirectoryRepository directoryRepository,
        IFileScanner fileScanner,
        IBackgroundJobClient jobClient,
        ILogger<LibraryScannerService> logger,
        IEnumerable<Resolvers.IItemResolver> itemResolvers,
        IBackgroundJobClient metadataJobClient
    )
    {
        this.libraryRepository = libraryRepository;
        this.scanRepository = scanRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.mediaPartRepository = mediaPartRepository;
        this.directoryRepository = directoryRepository;
        this.fileScanner = fileScanner;
        this.jobClient = jobClient;
        this.logger = logger;
        this.itemResolvers = itemResolvers;
        this.metadataJobClient = metadataJobClient;
    }

    /// <inheritdoc />
    public async Task<int> StartScanAsync(int libraryId)
    {
        var library = await this.libraryRepository.GetByIdAsync(libraryId);

        Guard.Against.Null(
            library,
            exceptionCreator: () => new InvalidOperationException($"Library {libraryId} not found")
        );

        // Check if scan already running
        var activeScan = await this.scanRepository.GetActiveScanAsync(libraryId);
        if (activeScan != null)
        {
            this.LogScanAlreadyRunning(libraryId);
            return activeScan.Id;
        }

        var scan = new LibraryScan
        {
            LibrarySectionId = libraryId,
            Status = LibraryScanStatus.Pending,
            StartedAt = DateTime.UtcNow,
        };

        scan = await this.scanRepository.AddAsync(scan);

        this.jobClient.Enqueue<LibraryScannerService>(s => s.ExecuteScanAsync(scan.Id));

        this.LogScanStarted(scan.Id, libraryId);

        return scan.Id;
    }

    /// <summary>
    /// Executes a library scan operation for the specified scan identifier.
    /// </summary>
    /// <param name="scanId">The identifier of the scan to execute.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Queue("scans")]
    [AutomaticRetry(Attempts = 0)]
    [MaximumConcurrentExecutions(1, timeoutInSeconds: 3600, pollingIntervalInSeconds: 10)]
    public async Task ExecuteScanAsync(int scanId)
    {
        var stopwatch = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        var scan = await this.scanRepository.GetByIdAsync(scanId);

        if (scan == null)
        {
            this.LogScanNotFound(scanId);
            return;
        }

        try
        {
            // Register cancellation token
            RunningScans.TryAdd(scanId, cts);

            // Update status to running
            scan.Status = LibraryScanStatus.Running;
            await this.scanRepository.UpdateAsync(scan);

            // Execute scan
            await this.PerformScanAsync(scan, cts.Token);

            // Mark complete
            scan.Status = LibraryScanStatus.Completed;
            scan.CompletedAt = DateTime.UtcNow;
            await this.scanRepository.UpdateAsync(scan);

            // Update library last scanned
            var library = await this.libraryRepository.GetByIdAsync(scan.LibrarySectionId);
            if (library != null)
            {
                library.LastScannedAt = DateTime.UtcNow;
                await this.libraryRepository.UpdateAsync(library);
            }

            this.LogScanCompleted(scanId, scan.ItemsAdded, scan.ItemsUpdated, scan.ItemsRemoved);
        }
        catch (OperationCanceledException)
        {
            scan.Status = LibraryScanStatus.Cancelled;
            scan.CompletedAt = DateTime.UtcNow;
            await this.scanRepository.UpdateAsync(scan);

            this.LogScanCancelled(scanId);
        }
        catch (Exception ex)
        {
            scan.Status = LibraryScanStatus.Failed;
            scan.CompletedAt = DateTime.UtcNow;
            scan.ErrorMessage = ex.Message;
            await this.scanRepository.UpdateAsync(scan);

            this.LogScanFailed(scanId, ex);
        }
        finally
        {
            stopwatch.Stop();
            this.LogScanDuration(scanId, stopwatch.ElapsedMilliseconds);
            RunningScans.TryRemove(scanId, out _);
            cts?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task CancelScanAsync(int scanId)
    {
        if (RunningScans.TryGetValue(scanId, out var cts))
        {
            cts.Cancel();
            this.LogScanCancelling(scanId);
        }
        else
        {
            this.LogScanNotRunning(scanId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IQueryable<LibraryScan> GetScanHistoryQueryable(int libraryId)
    {
        return this.scanRepository.GetQueryable().Where(scan => scan.LibrarySectionId == libraryId);
    }

    /// <inheritdoc />
    public async Task<LibraryScan?> GetScanStatusAsync(int scanId)
    {
        Guard.Against.Null(scanId);
        Guard.Against.Negative(scanId);

        var scan = await this.scanRepository.GetByIdAsync(scanId);
        if (scan == null)
        {
            return null;
        }

        return scan;
    }

    // Removed GetSupportedExtensions; extension filtering happens via resolvers now.
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Started scan {ScanId} for library {LibraryId}"
    )]
    private partial void LogScanStarted(int scanId, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scan already running for library {LibraryId}"
    )]
    private partial void LogScanAlreadyRunning(int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scan {ScanId} not found")]
    private partial void LogScanNotFound(int scanId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scan {ScanId} completed: {Added} added, {Updated} updated, {Removed} removed"
    )]
    private partial void LogScanCompleted(int scanId, int added, int updated, int removed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scan {ScanId} was cancelled")]
    private partial void LogScanCancelled(int scanId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scan {ScanId} failed")]
    private partial void LogScanFailed(int scanId, Exception? ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelling scan {ScanId}")]
    private partial void LogScanCancelling(int scanId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scan {ScanId} is not currently running")]
    private partial void LogScanNotRunning(int scanId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scanning folder: {Path}")]
    private partial void LogScanningFolder(string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {FileCount} files in library {LibraryId}"
    )]
    private partial void LogFoundFiles(int fileCount, int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing file: {Path}")]
    private partial void LogErrorProcessingFile(string path, Exception? ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scan {ScanId} finished in {ElapsedMs} ms"
    )]
    private partial void LogScanDuration(int scanId, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping unclaimed file: {Path}")]
    private partial void LogFileUnclaimed(string path);

    private async Task PerformScanAsync(LibraryScan scan, CancellationToken cancellationToken)
    {
        // Reference to injected scanner to avoid unused field analyzer until streaming scanner uses it in future
        _ = this.fileScanner;

        var library = await this.libraryRepository.GetByIdWithFoldersAsync(scan.LibrarySectionId);
        if (library == null)
        {
            throw new InvalidOperationException($"Library {scan.LibrarySectionId} not found");
        }

        // Serialize all DbContext usage across background tasks to avoid EF Core concurrency exceptions.
        var dbLock = new SemaphoreSlim(1, 1);

        // Create a producer-consumer pipeline using channels so directories are processed as soon as they finish scanning.
        var channelOptions = new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        };
        var channel = Channel.CreateBounded<(
            SectionLocation Location,
            ScannedDirectoryBatch Batch
        )>(channelOptions);

        // Step 1: Kick off scanning tasks per SectionLocation (producer side). As each directory batch is discovered
        // by the file scanner, write it to the channel tagged with its SectionLocation.
        var producerTasks = new List<Task>(library.Locations.Count);
        foreach (var location in library.Locations)
        {
            this.LogScanningFolder(location.RootPath);

            var task = Task.Run(
                async () =>
                {
                    await foreach (
                        var batch in this.fileScanner.ScanDirectoryStreamingAsync(
                            location.RootPath,
                            cancellationToken
                        )
                    )
                    {
                        await channel.Writer.WriteAsync((location, batch), cancellationToken);
                    }
                },
                cancellationToken
            );

            producerTasks.Add(task);
        }

        // Ensure channel is completed when all producers finish
        var completionTask = Task.Run(
            async () =>
            {
                try
                {
                    await Task.WhenAll(producerTasks);
                    channel.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                }
            },
            cancellationToken
        );

        // Step 2: No longer pre-load existing paths to avoid high memory usage.
        // We'll stream them during the removal phase after insertion completes.

        // Step 3: Consume batches as they arrive and process without blocking scanning
        const int batchSize = 200;
        var addedCount = 0;
        const int updatedCount = 0; // reserved for future updates logic
        var removedCount = 0;

        // Work pipeline:
        // entriesChannel (location, directory batch) -> dispatcher -> workChannel (location, file)
        // workChannel -> N consumers -> insertChannel (metadata item) -> single inserter (bulk insert)
        var scannedPaths = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        var workChannel = Channel.CreateBounded<(
            SectionLocation Location,
            Resolvers.FileSystemMetadata File
        )>(
            new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true,
            }
        );

        var insertChannel = Channel.CreateBounded<MetadataItem>(
            new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }
        );

        // Throttle progress persistence
        const int progressEveryNFiles = 1000;
        const long progressEveryMs = 2000;
        long lastPersistTicks = Stopwatch.GetTimestamp();
        static bool ShouldPersist(int processedFiles, long lastTicks)
        {
            if (processedFiles != 0 && processedFiles % progressEveryNFiles == 0)
            {
                return true;
            }

            var now = Stopwatch.GetTimestamp();
            var elapsedMs = (now - lastTicks) * 1000 / Stopwatch.Frequency;
            return elapsedMs >= progressEveryMs;
        }

        // Dispatcher: flatten directory batches to per-file work items AND persist directories incrementally.
        var dispatchTask = Task.Run(
            async () =>
            {
                // Maintain in-memory maps of relativePath -> DirectoryId per SectionLocation
                var idsByLocation = new Dictionary<int, Dictionary<string, int>>();

                async Task<int> EnsureDirectoryAsync(SectionLocation location, string absDirPath)
                {
                    // Get or create map for this location
                    if (!idsByLocation.TryGetValue(location.Id, out var idByRel))
                    {
                        idByRel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        idsByLocation[location.Id] = idByRel;
                    }

                    static string Normalize(string s) => s.Replace('\\', '/');

                    // Compute normalized relative path; root -> ""
                    var rel = Path.GetRelativePath(location.RootPath, absDirPath);
                    if (rel == "." || string.IsNullOrWhiteSpace(rel))
                    {
                        rel = string.Empty;
                    }
                    else
                    {
                        rel = Normalize(rel);
                    }

                    // Ensure root exists first
                    if (!idByRel.ContainsKey(string.Empty))
                    {
                        var root = new Core.Entities.Directory
                        {
                            LibrarySectionId = library.Id,
                            ParentDirectoryId = null,
                            Path = string.Empty,
                        };

                        await dbLock.WaitAsync(cancellationToken);
                        try
                        {
                            await this.directoryRepository.BulkInsertAsync(
                                new[] { root },
                                cancellationToken
                            );
                            idByRel[string.Empty] = root.Id;
                        }
                        finally
                        {
                            dbLock.Release();
                        }
                    }

                    // If already known, return fast
                    if (idByRel.TryGetValue(rel, out var knownId))
                    {
                        return knownId;
                    }

                    // Walk the chain and insert any missing segments
                    var segments = string.IsNullOrEmpty(rel)
                        ? Array.Empty<string>()
                        : rel.Split('/', StringSplitOptions.RemoveEmptyEntries);

                    var pathSoFar = string.Empty;
                    var pathBuilder = new System.Text.StringBuilder();
                    var parentId = idByRel[string.Empty];

                    for (var i = 0; i < segments.Length; i++)
                    {
                        if (pathBuilder.Length > 0)
                        {
                            pathBuilder.Append('/');
                        }

                        pathBuilder.Append(segments[i]);
                        pathSoFar = pathBuilder.ToString();
                        if (idByRel.TryGetValue(pathSoFar, out var id))
                        {
                            parentId = id;
                            continue;
                        }

                        // If possibly inserted previously (e.g., by another scan of another subtree), re-check DB
                        // Insert missing level with known parent id and store only the segment as Path
                        var entity = new Core.Entities.Directory
                        {
                            LibrarySectionId = library.Id,
                            ParentDirectoryId = parentId,
                            Path = segments[i],
                        };

                        await dbLock.WaitAsync(cancellationToken);
                        try
                        {
                            await this.directoryRepository.BulkInsertAsync(
                                new[] { entity },
                                cancellationToken
                            );
                            idByRel[pathSoFar] = entity.Id;
                            parentId = entity.Id;
                        }
                        finally
                        {
                            dbLock.Release();
                        }
                    }

                    return idByRel[rel];
                }

                await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    // Track totals and progress per batch (serialize modifications + DB)
                    await dbLock.WaitAsync(cancellationToken);
                    try
                    {
                        scan.TotalFiles += entry.Batch.Files.Count;
                        if (ShouldPersist(scan.ProcessedFiles, lastPersistTicks))
                        {
                            await this.scanRepository.UpdateAsync(scan);
                            lastPersistTicks = Stopwatch.GetTimestamp();
                        }
                    }
                    finally
                    {
                        dbLock.Release();
                    }

                    // Ensure the directory (and any missing ancestors) are persisted immediately
                    await EnsureDirectoryAsync(entry.Location, entry.Batch.DirectoryPath);

                    foreach (var file in entry.Batch.Files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        scannedPaths.TryAdd(file.Path, 0);
                        await workChannel.Writer.WriteAsync(
                            (entry.Location, file),
                            cancellationToken
                        );
                    }
                }

                workChannel.Writer.TryComplete();
            },
            cancellationToken
        );

        // Consumers: build metadata items in parallel and hand off to inserter
        var consumerCount = Math.Min(8, Math.Max(2, Environment.ProcessorCount));
        var consumerTasks = new List<Task>(consumerCount);

        for (var i = 0; i < consumerCount; i++)
        {
            consumerTasks.Add(
                Task.Run(
                    async () =>
                    {
                        await foreach (
                            var work in workChannel.Reader.ReadAllAsync(cancellationToken)
                        )
                        {
                            try
                            {
                                // Use resolver pipeline (ordered by priority) to classify + build MetadataItem
                                var fileMeta = work.File; // Already a FileSystemMetadata
                                // For simple resolvers we skip children enumeration unless directory.
                                IReadOnlyList<Resolvers.FileSystemMetadata>? children = null;
                                if (fileMeta.IsDirectory)
                                {
                                    try
                                    {
                                        children = System
                                            .IO.Directory.EnumerateFileSystemEntries(fileMeta.Path)
                                            .Select(Resolvers.FileSystemMetadata.FromPath)
                                            .ToList();
                                    }
                                    catch
                                    {
                                        children = null;
                                    }
                                }

                                var args = new Resolvers.ItemResolveArgs(
                                    fileMeta,
                                    library.Type,
                                    work.Location.Id,
                                    library.Id,
                                    children,
                                    IsRoot: string.Equals(
                                        Path.GetFullPath(work.Location.RootPath)
                                            .TrimEnd(
                                                Path.DirectorySeparatorChar,
                                                Path.AltDirectorySeparatorChar
                                            ),
                                        Path.GetFullPath(work.File.Path)
                                            .TrimEnd(
                                                Path.DirectorySeparatorChar,
                                                Path.AltDirectorySeparatorChar
                                            ),
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                );

                                MetadataItem? resolved = null;
                                foreach (
                                    var resolver in this.itemResolvers.OrderBy(r => r.Priority)
                                )
                                {
                                    resolved = resolver.Resolve(args);
                                    if (resolved != null)
                                    {
                                        break;
                                    }
                                }

                                if (resolved == null)
                                {
                                    this.LogFileUnclaimed(work.File.Path);
                                    continue; // Skip inserting unclaimed items
                                }

                                await insertChannel.Writer.WriteAsync(resolved, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                this.LogErrorProcessingFile(work.File.Path, ex);
                            }
                        }
                    },
                    cancellationToken
                )
            );
        }

        // Inserter: single writer to DB in batches
        var inserterTask = Task.Run(
            async () =>
            {
                var pending = new List<MetadataItem>(batchSize);

                await foreach (var item in insertChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    pending.Add(item);
                    if (pending.Count >= batchSize)
                    {
                        await dbLock.WaitAsync(cancellationToken);
                        try
                        {
                            await this.metadataItemRepository.BulkInsertAsync(
                                pending,
                                cancellationToken
                            );
                            // Enqueue metadata refresh for each inserted item in this batch without extra DB queries.
                            foreach (var inserted in pending)
                            {
                                this.metadataJobClient.Enqueue<MetadataService>(svc =>
                                    svc.RefreshMetadataAsync(inserted.Uuid)
                                );
                            }

                            addedCount += pending.Count;
                            scan.ProcessedFiles += pending.Count;
                            pending.Clear();
                            await this.scanRepository.UpdateAsync(scan);
                            lastPersistTicks = Stopwatch.GetTimestamp();
                        }
                        finally
                        {
                            dbLock.Release();
                        }
                    }
                }

                if (pending.Count > 0)
                {
                    await dbLock.WaitAsync(cancellationToken);
                    try
                    {
                        await this.metadataItemRepository.BulkInsertAsync(
                            pending,
                            cancellationToken
                        );
                        // Enqueue metadata refresh for each inserted item in this final batch.
                        foreach (var inserted in pending)
                        {
                            this.metadataJobClient.Enqueue<MetadataService>(svc =>
                                svc.RefreshMetadataAsync(inserted.Uuid)
                            );
                        }

                        addedCount += pending.Count;
                        scan.ProcessedFiles += pending.Count;
                        pending.Clear();
                        await this.scanRepository.UpdateAsync(scan);
                        lastPersistTicks = Stopwatch.GetTimestamp();
                    }
                    finally
                    {
                        dbLock.Release();
                    }
                }
            },
            cancellationToken
        );

        // Ensure pipeline completion in order
        await completionTask; // producers finished, entries channel completed
        await dispatchTask; // dispatcher finished, work channel completed

        // Signal inserter completion after all consumers have finished producing items
        await Task.WhenAll(consumerTasks);
        insertChannel.Writer.TryComplete();

        // Finish insertion
        await inserterTask;

        this.LogFoundFiles(scannedPaths.Count, library.Id);

        // Step 4: Remove items no longer on filesystem (stream to avoid loading all existing paths)
        const int deleteChunkSize = 2000;
        var toDelete = new List<string>(deleteChunkSize);

        await foreach (
            var existingPath in this
                .mediaPartRepository.StreamFilePathsByLibraryIdAsync(library.Id, cancellationToken)
                .WithCancellation(cancellationToken)
        )
        {
            if (!scannedPaths.ContainsKey(existingPath))
            {
                toDelete.Add(existingPath);
                if (toDelete.Count >= deleteChunkSize)
                {
                    await dbLock.WaitAsync(cancellationToken);
                    try
                    {
                        removedCount += await this.mediaPartRepository.DeleteByFilePathsAsync(
                            toDelete,
                            cancellationToken
                        );
                        toDelete.Clear();
                    }
                    finally
                    {
                        dbLock.Release();
                    }
                }
            }
        }

        if (toDelete.Count > 0)
        {
            await dbLock.WaitAsync(cancellationToken);
            try
            {
                removedCount += await this.mediaPartRepository.DeleteByFilePathsAsync(
                    toDelete,
                    cancellationToken
                );
                toDelete.Clear();
            }
            finally
            {
                dbLock.Release();
            }
        }

        // Update scan summary once (serialize DB access)
        await dbLock.WaitAsync(cancellationToken);
        try
        {
            scan.ItemsAdded = addedCount;
            scan.ItemsUpdated = updatedCount;
            scan.ItemsRemoved = removedCount;
            await this.scanRepository.UpdateAsync(scan);
        }
        finally
        {
            dbLock.Release();
        }
    }
}
