// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;

using Ardalis.GuardClauses;

using Hangfire;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services.Pipeline;
using NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

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
    private readonly IBackgroundJobClient jobClient;
    private readonly ILogger<LibraryScannerService> logger;
    private readonly IBackgroundJobClient metadataJobClient;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IJobProgressReporter jobProgressReporter;
    private readonly DirectoryTraversalStage directoryTraversalStage;
    private readonly ChangeDetectionStage changeDetectionStage;
    private readonly ResolveItemsStage resolveItemsStage;
    private readonly LocalMetadataStage localMetadataStage;
    private readonly IMetadataDeduplicationService metadataDeduplicationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryScannerService"/> class.
    /// </summary>
    /// <param name="libraryRepository">The library repository for data access.</param>
    /// <param name="scanRepository">The library scan repository for scan data access.</param>
    /// <param name="metadataItemRepository">The metadata item repository for metadata access.</param>
    /// <param name="mediaPartRepository">The media part repository for media part access.</param>
    /// <param name="jobClient">The background job client for scheduling tasks.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="metadataJobClient">The Hangfire client for queuing metadata jobs.</param>
    /// <param name="dbContextFactory">Factory for creating database contexts.</param>
    /// <param name="jobProgressReporter">Reporter for job progress notifications.</param>
    /// <param name="directoryTraversalStage">Pipeline stage that enumerates library locations.</param>
    /// <param name="changeDetectionStage">Pipeline stage that marks unchanged files.</param>
    /// <param name="resolveItemsStage">Pipeline stage that resolves files to domain items.</param>
    /// <param name="localMetadataStage">Pipeline stage that extracts local metadata.</param>
    /// <param name="metadataDeduplicationService">Service for metadata deduplication cache management.</param>
    public LibraryScannerService(
        ILibrarySectionRepository libraryRepository,
        ILibraryScanRepository scanRepository,
        IMetadataItemRepository metadataItemRepository,
        IMediaPartRepository mediaPartRepository,
        IBackgroundJobClient jobClient,
        ILogger<LibraryScannerService> logger,
        IBackgroundJobClient metadataJobClient,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IJobProgressReporter jobProgressReporter,
        DirectoryTraversalStage directoryTraversalStage,
        ChangeDetectionStage changeDetectionStage,
        ResolveItemsStage resolveItemsStage,
        LocalMetadataStage localMetadataStage,
        IMetadataDeduplicationService metadataDeduplicationService
    )
    {
        this.libraryRepository = libraryRepository;
        this.scanRepository = scanRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.mediaPartRepository = mediaPartRepository;
        this.jobClient = jobClient;
        this.logger = logger;
        this.metadataJobClient = metadataJobClient;
        this.dbContextFactory = dbContextFactory;
        this.jobProgressReporter = jobProgressReporter;
        this.directoryTraversalStage = directoryTraversalStage;
        this.changeDetectionStage = changeDetectionStage;
        this.resolveItemsStage = resolveItemsStage;
        this.localMetadataStage = localMetadataStage;
        this.metadataDeduplicationService = metadataDeduplicationService;
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
            LogScanAlreadyRunning(this.logger, libraryId);
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

        LogScanStarted(this.logger, scan.Id, libraryId);

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
            LogScanNotFound(this.logger, scanId);
            return;
        }

        try
        {
            // Register cancellation token
            RunningScans.TryAdd(scanId, cts);

            // Update status to running
            scan.Status = LibraryScanStatus.Running;
            await this.scanRepository.UpdateAsync(scan);

            // Report scan started to unified notification system
            await this.jobProgressReporter.StartAsync(
                scan.LibrarySectionId,
                JobType.LibraryScan,
                0,
                cts.Token
            );

            // Execute scan
            await this.PerformScanAsync(scan, cts.Token);

            // Mark complete
            scan.Status = LibraryScanStatus.Completed;
            scan.CompletedAt = DateTime.UtcNow;
            await this.scanRepository.UpdateAsync(scan);

            // Report completion to unified notification system
            await this.jobProgressReporter.CompleteAsync(
                scan.LibrarySectionId,
                JobType.LibraryScan,
                cts.Token
            );

            // Update library last scanned
            var library = await this.libraryRepository.GetByIdAsync(scan.LibrarySectionId);
            if (library != null)
            {
                library.LastScannedAt = DateTime.UtcNow;
                await this.libraryRepository.UpdateAsync(library);
            }

            LogScanCompleted(
                this.logger,
                scanId,
                scan.ItemsAdded,
                scan.ItemsUpdated,
                scan.ItemsRemoved
            );
        }
        catch (OperationCanceledException)
        {
            scan.Status = LibraryScanStatus.Cancelled;
            scan.CompletedAt = DateTime.UtcNow;
            await this.scanRepository.UpdateAsync(scan);

            // Report cancellation to unified notification system
            await this.jobProgressReporter.FailAsync(
                scan.LibrarySectionId,
                JobType.LibraryScan,
                "Scan was cancelled"
            );

            LogScanCancelled(this.logger, scanId);
        }
        catch (Exception ex)
        {
            scan.Status = LibraryScanStatus.Failed;
            scan.CompletedAt = DateTime.UtcNow;
            scan.ErrorMessage = ex.Message;
            await this.scanRepository.UpdateAsync(scan);

            // Report failure to unified notification system
            await this.jobProgressReporter.FailAsync(
                scan.LibrarySectionId,
                JobType.LibraryScan,
                ex.Message
            );

            LogScanFailed(this.logger, scanId, ex);
        }
        finally
        {
            stopwatch.Stop();
            LogScanDuration(this.logger, scanId, stopwatch.ElapsedMilliseconds);
            RunningScans.TryRemove(scanId, out _);
            cts.Dispose();
        }
    }

    /// <inheritdoc />
    public Task CancelScanAsync(int scanId)
    {
        if (RunningScans.TryGetValue(scanId, out var cts))
        {
            cts.Cancel();
            LogScanCancelling(this.logger, scanId);
        }
        else
        {
            LogScanNotRunning(this.logger, scanId);
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<LibraryScan>> GetInterruptedScansAsync()
    {
        return await this
            .scanRepository.GetQueryable()
            .Where(s =>
                s.Status == LibraryScanStatus.Running
                && s.CurrentStage != null
                && s.ResumeCursor != null
            )
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ResumeScanAsync(int scanId)
    {
        var scan = await this.scanRepository.GetByIdAsync(scanId);
        if (scan == null)
        {
            LogScanNotFound(this.logger, scanId);
            return;
        }

        if (scan.Status != LibraryScanStatus.Running || scan.CurrentStage == null)
        {
            LogScanNotInterrupted(this.logger, scanId);
            return;
        }

        LogResumingScan(this.logger, scanId, scan.CurrentStage, scan.ResumeCursor);

        // Re-queue the scan job using the existing execution method
        this.jobClient.Enqueue<LibraryScannerService>(s => s.ExecuteScanAsync(scan.Id));
    }

    /// <inheritdoc />
    public Task ScanPathAsync(
        int libraryId,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        // For now, micro-scans trigger a full library scan.
        // A future optimization would process only the specific paths.
        // The full scan will detect changes via ChangeDetectionStage and
        // only process modified files.
        LogMicroScanPath(this.logger, path, libraryId);
        this.jobClient.Enqueue<LibraryScannerService>(s => s.StartScanAsync(libraryId));
        return Task.CompletedTask;
    }

    // Removed GetSupportedExtensions; extension filtering happens via resolvers now.
    private static Core.DTOs.ScanCheckpoint? BuildCheckpointFromScan(LibraryScan scan)
    {
        if (string.IsNullOrEmpty(scan.CurrentStage))
        {
            return null;
        }

        return new Core.DTOs.ScanCheckpoint(
            scan.CurrentStage,
            scan.ResumeCursor,
            scan.CheckpointVersion
        );
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Started scan {ScanId} for library {LibraryId}"
    )]
    private static partial void LogScanStarted(ILogger logger, int scanId, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scan already running for library {LibraryId}"
    )]
    private static partial void LogScanAlreadyRunning(ILogger logger, int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scan {ScanId} not found")]
    private static partial void LogScanNotFound(ILogger logger, int scanId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scan {ScanId} is not in an interrupted state, cannot resume"
    )]
    private static partial void LogScanNotInterrupted(ILogger logger, int scanId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Resuming interrupted scan {ScanId} from stage {Stage} at cursor {Cursor}"
    )]
    private static partial void LogResumingScan(
        ILogger logger,
        int scanId,
        string stage,
        string? cursor
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scan {ScanId} completed: {Added} added, {Updated} updated, {Removed} removed"
    )]
    private static partial void LogScanCompleted(
        ILogger logger,
        int scanId,
        int added,
        int updated,
        int removed
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Scan {ScanId} was cancelled")]
    private static partial void LogScanCancelled(ILogger logger, int scanId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scan {ScanId} failed")]
    private static partial void LogScanFailed(ILogger logger, int scanId, Exception? ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelling scan {ScanId}")]
    private static partial void LogScanCancelling(ILogger logger, int scanId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scan {ScanId} is not currently running")]
    private static partial void LogScanNotRunning(ILogger logger, int scanId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scanning folder: {Path}")]
    private static partial void LogScanningFolder(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {FileCount} files in library {LibraryId}"
    )]
    private static partial void LogFoundFiles(ILogger logger, int fileCount, int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing file: {Path}")]
    private static partial void LogErrorProcessingFile(ILogger logger, string path, Exception? ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scan {ScanId} finished in {ElapsedMs} ms"
    )]
    private static partial void LogScanDuration(ILogger logger, int scanId, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping unclaimed file: {Path}")]
    private static partial void LogFileUnclaimed(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Building scan pipeline for library {LibraryId} (scan {ScanId})"
    )]
    private static partial void LogBuildingScanPipeline(ILogger logger, int libraryId, int scanId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Triggering scan for path {Path} in library {LibraryId}"
    )]
    private static partial void LogMicroScanPath(ILogger logger, string path, int libraryId);

    private async Task PerformScanAsync(LibraryScan scan, CancellationToken cancellationToken)
    {
        var library = await this.libraryRepository.GetByIdWithFoldersAsync(scan.LibrarySectionId);
        if (library == null)
        {
            throw new InvalidOperationException($"Library {scan.LibrarySectionId} not found");
        }

        LogBuildingScanPipeline(this.logger, library.Id, scan.Id);

        const int batchSize = 200;
        const int progressReportInterval = 50;
        const int gcPressureReliefInterval = 10000; // Trigger GC every 10k files to prevent OOM
        const int updatedCount = 0; // reserved for future updates logic
        var addedCount = 0;
        var removedCount = 0;
        var pending = new List<MetadataBaseItem>(batchSize);

        // Use a more memory-efficient approach for tracking scanned paths in large libraries:
        // Instead of keeping all paths in memory, we'll mark them as seen in the existing
        // paths set (which we already loaded), then remaining unmarked paths are deletions.
        // First, get the existing paths from cache
        var existingPathsForTracking =
            this.changeDetectionStage.GetCachedPaths(library.Id)
            ?? await this.mediaPartRepository.GetFilePathsByLibraryIdAsync(
                library.Id,
                cancellationToken
            );

        var lastProgressReport = 0;

        var pipeline = ScanPipeline
            .From(library.Locations.ToAsyncEnumerable())
            .Then(this.directoryTraversalStage)
            .Then(this.changeDetectionStage)
            .Then(this.resolveItemsStage)
            .Then(this.localMetadataStage);

        var checkpoint = BuildCheckpointFromScan(scan);

        var context = new ScanPipelineContext(
            library,
            scan,
            this.dbContextFactory,
            progressReporter: null,
            checkpoint: checkpoint
        );

        await foreach (
            var item in pipeline
                .Build(context, cancellationToken)
                .WithCancellation(cancellationToken)
        )
        {
            scan.TotalFiles++;

            // Remove from existing paths set to mark as "still present"
            // This is memory-efficient as we modify the existing set in-place
            existingPathsForTracking.Remove(item.File.Path);

            // Report progress every 50 files to avoid excessive updates
            if (scan.TotalFiles - lastProgressReport >= progressReportInterval)
            {
                lastProgressReport = scan.TotalFiles;
                await this.jobProgressReporter.ReportProgressAsync(
                    scan.LibrarySectionId,
                    JobType.LibraryScan,
                    scan.TotalFiles,
                    scan.TotalFiles, // Total not known upfront, use current count
                    cancellationToken
                );
            }

            // Periodically trigger GC to prevent OOM on large libraries.
            // This is a trade-off: slightly slower scans but prevents the OS from killing the process.
            // S1215: GC.Collect is intentional here to prevent OOM during large library scans.
#pragma warning disable S1215
            if (scan.TotalFiles % gcPressureReliefInterval == 0)
            {
                GC.Collect(generation: 1, mode: GCCollectionMode.Optimized, blocking: false);
            }
#pragma warning restore S1215

            if (item.IsUnchanged)
            {
                continue;
            }

            if (item.ResolvedMetadata == null)
            {
                LogFileUnclaimed(this.logger, item.File.Path);
                continue;
            }

            pending.Add(item.ResolvedMetadata);

            if (pending.Count < batchSize)
            {
                continue;
            }

            await this.InsertBatchAsync(pending, cancellationToken);
            addedCount += pending.Count;
            pending.Clear();
        }

        if (pending.Count > 0)
        {
            await this.InsertBatchAsync(pending, cancellationToken);
            addedCount += pending.Count;
            pending.Clear();
        }

        // Reuse cached paths from ChangeDetectionStage to avoid duplicate database query
        // At this point, existingPathsForTracking contains only files that were NOT scanned
        // (we removed all scanned files from the set during iteration)
        var deleteChunk = new List<string>(2000);

        foreach (var pathToDelete in existingPathsForTracking)
        {
            deleteChunk.Add(pathToDelete);
            if (deleteChunk.Count >= 2000)
            {
                removedCount += await this.mediaPartRepository.SoftDeleteByFilePathsAsync(
                    deleteChunk,
                    cancellationToken
                );
                deleteChunk.Clear();
            }
        }

        if (deleteChunk.Count > 0)
        {
            removedCount += await this.mediaPartRepository.SoftDeleteByFilePathsAsync(
                deleteChunk,
                cancellationToken
            );
            deleteChunk.Clear();
        }

        // Clear the cache to free memory after scan completes
        this.changeDetectionStage.ClearCache(library.Id);

        // Clear metadata deduplication cache to prevent unbounded growth
        this.metadataDeduplicationService.ClearCache();

        // Clear the tracking set to free memory
        existingPathsForTracking.Clear();

        scan.ItemsAdded = addedCount;
        scan.ItemsUpdated = updatedCount;
        scan.ItemsRemoved = removedCount;
        await this.scanRepository.UpdateAsync(scan);

        // Schedule collection image generation after a delay to allow metadata refresh jobs
        // to complete and generate child thumbnails. This job will create collage thumbnails
        // for collection-type items (PhotoAlbum, PictureSet, etc.) in the library.
        this.jobClient.Schedule<ICollectionImageOrchestrator>(
            svc => svc.GenerateCollectionImagesForLibraryAsync(library.Id, CancellationToken.None),
            TimeSpan.FromMinutes(2)
        );
    }

    private async Task InsertBatchAsync(
        List<MetadataBaseItem> batch,
        CancellationToken cancellationToken
    )
    {
        // Pre-size list based on batch size (assumes ~2x expansion for children on average)
        var flattened = new List<MetadataBaseItem>(batch.Count * 2);
        foreach (var item in batch)
        {
            FlattenMetadataLocal(item, flattened);
        }

        await this.metadataItemRepository.BulkInsertAsync(batch, cancellationToken);

        // Enqueue metadata refresh jobs for each unique UUID
        foreach (var uuid in flattened.Select(m => m.Uuid).Distinct())
        {
            this.metadataJobClient.Enqueue<MetadataService>(svc =>
                svc.RefreshMetadataAsync(uuid)
            );
        }

        // Clear to help GC reclaim memory faster
        flattened.Clear();

        static void FlattenMetadataLocal(
            MetadataBaseItem item,
            ICollection<MetadataBaseItem> flatList
        )
        {
            if (item.Uuid == Guid.Empty)
            {
                item.Uuid = Guid.NewGuid();
            }

            flatList.Add(item);

            if (item.Children is { Count: > 0 })
            {
                foreach (var child in item.Children)
                {
                    FlattenMetadataLocal(child, flatList);
                }
            }
        }
    }
}
