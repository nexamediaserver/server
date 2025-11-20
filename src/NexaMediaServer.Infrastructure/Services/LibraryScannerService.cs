// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ardalis.GuardClauses;
using Hangfire;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services.Pipeline;
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
    private readonly DirectoryTraversalStage directoryTraversalStage;
    private readonly ChangeDetectionStage changeDetectionStage;
    private readonly ResolveItemsStage resolveItemsStage;
    private readonly LocalMetadataStage localMetadataStage;

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
    /// <param name="directoryTraversalStage">Pipeline stage that enumerates library locations.</param>
    /// <param name="changeDetectionStage">Pipeline stage that marks unchanged files.</param>
    /// <param name="resolveItemsStage">Pipeline stage that resolves files to domain items.</param>
    /// <param name="localMetadataStage">Pipeline stage that extracts local metadata.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "Constructor injection keeps dependencies explicit"
    )]
    public LibraryScannerService(
        ILibrarySectionRepository libraryRepository,
        ILibraryScanRepository scanRepository,
        IMetadataItemRepository metadataItemRepository,
        IMediaPartRepository mediaPartRepository,
        IBackgroundJobClient jobClient,
        ILogger<LibraryScannerService> logger,
        IBackgroundJobClient metadataJobClient,
        DirectoryTraversalStage directoryTraversalStage,
        ChangeDetectionStage changeDetectionStage,
        ResolveItemsStage resolveItemsStage,
        LocalMetadataStage localMetadataStage
    )
    {
        this.libraryRepository = libraryRepository;
        this.scanRepository = scanRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.mediaPartRepository = mediaPartRepository;
        this.jobClient = jobClient;
        this.logger = logger;
        this.metadataJobClient = metadataJobClient;
        this.directoryTraversalStage = directoryTraversalStage;
        this.changeDetectionStage = changeDetectionStage;
        this.resolveItemsStage = resolveItemsStage;
        this.localMetadataStage = localMetadataStage;
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

            LogScanCancelled(this.logger, scanId);
        }
        catch (Exception ex)
        {
            scan.Status = LibraryScanStatus.Failed;
            scan.CompletedAt = DateTime.UtcNow;
            scan.ErrorMessage = ex.Message;
            await this.scanRepository.UpdateAsync(scan);

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

    // Removed GetSupportedExtensions; extension filtering happens via resolvers now.
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

    private async Task PerformScanAsync(LibraryScan scan, CancellationToken cancellationToken)
    {
        var library = await this.libraryRepository.GetByIdWithFoldersAsync(scan.LibrarySectionId);
        if (library == null)
        {
            throw new InvalidOperationException($"Library {scan.LibrarySectionId} not found");
        }

        LogBuildingScanPipeline(this.logger, library.Id, scan.Id);

        const int batchSize = 200;
        const int updatedCount = 0; // reserved for future updates logic
        var addedCount = 0;
        var removedCount = 0;
        var pending = new List<MetadataBaseItem>(batchSize);
        var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pipeline = ScanPipeline
            .From(library.Locations.ToAsyncEnumerable())
            .Then(this.directoryTraversalStage)
            .Then(this.changeDetectionStage)
            .Then(this.resolveItemsStage)
            .Then(this.localMetadataStage);

        var context = new ScanPipelineContext(library, scan);

        await foreach (
            var item in pipeline
                .Build(context, cancellationToken)
                .WithCancellation(cancellationToken)
        )
        {
            scannedPaths.Add(item.File.Path);
            scan.TotalFiles++;

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

            await this.InsertBatchAsync(pending, scan, cancellationToken);
            addedCount += pending.Count;
            pending.Clear();
        }

        if (pending.Count > 0)
        {
            await this.InsertBatchAsync(pending, scan, cancellationToken);
            addedCount += pending.Count;
            pending.Clear();
        }

        var deleteChunk = new List<string>(2000);
        foreach (
            var existing in await this.mediaPartRepository.GetFilePathsByLibraryIdAsync(
                library.Id,
                cancellationToken
            )
        )
        {
            if (scannedPaths.Contains(existing))
            {
                continue;
            }

            deleteChunk.Add(existing);
            if (deleteChunk.Count >= 2000)
            {
                removedCount += await this.mediaPartRepository.DeleteByFilePathsAsync(
                    deleteChunk,
                    cancellationToken
                );
                deleteChunk.Clear();
            }
        }

        if (deleteChunk.Count > 0)
        {
            removedCount += await this.mediaPartRepository.DeleteByFilePathsAsync(
                deleteChunk,
                cancellationToken
            );
            deleteChunk.Clear();
        }

        scan.ItemsAdded = addedCount;
        scan.ItemsUpdated = updatedCount;
        scan.ItemsRemoved = removedCount;
        await this.scanRepository.UpdateAsync(scan);
    }

    private async Task InsertBatchAsync(
        List<MetadataBaseItem> batch,
        LibraryScan scan,
        CancellationToken cancellationToken
    )
    {
        await this.metadataItemRepository.BulkInsertAsync(batch, cancellationToken);

        foreach (var item in batch)
        {
            this.metadataJobClient.Enqueue<MetadataService>(svc =>
                svc.RefreshMetadataAsync(item.Uuid)
            );
        }

        scan.ProcessedFiles += batch.Count;
        await this.scanRepository.UpdateAsync(scan);
    }
}
