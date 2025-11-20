// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Hangfire job that performs targeted micro-scans for filesystem changes detected by watchers.
/// </summary>
[Queue("scans")]
public sealed partial class MicroScanJob : IMicroScanJob
{
    private readonly ILogger<MicroScanJob> logger;
    private readonly ILibraryScannerService libraryScannerService;
    private readonly IMediaPartRepository mediaPartRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroScanJob"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="libraryScannerService">The library scanner service.</param>
    /// <param name="mediaPartRepository">The media part repository.</param>
    public MicroScanJob(
        ILogger<MicroScanJob> logger,
        ILibraryScannerService libraryScannerService,
        IMediaPartRepository mediaPartRepository
    )
    {
        this.logger = logger;
        this.libraryScannerService = libraryScannerService;
        this.mediaPartRepository = mediaPartRepository;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        CoalescedChangeEvent changeEvent,
        CancellationToken cancellationToken = default
    )
    {
        LogMicroScanStarted(
            this.logger,
            changeEvent.LibrarySectionId,
            changeEvent.PathsToScan.Count,
            changeEvent.PathsToRemove.Count
        );

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Process deletions first
            if (changeEvent.PathsToRemove.Count > 0)
            {
                await this.ProcessDeletionsAsync(changeEvent.PathsToRemove, cancellationToken);
            }

            // Then process additions/modifications via targeted scan
            if (changeEvent.PathsToScan.Count > 0)
            {
                await this.ProcessScansAsync(
                    changeEvent.LibrarySectionId,
                    changeEvent.PathsToScan,
                    cancellationToken
                );
            }

            stopwatch.Stop();
            LogMicroScanCompleted(
                this.logger,
                changeEvent.LibrarySectionId,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            LogMicroScanFailed(this.logger, changeEvent.LibrarySectionId, ex);
            throw;
        }
    }

    private async Task ProcessDeletionsAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken
    )
    {
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Soft-delete media parts matching this path (could be file or directory)
            var deletedCount = await this.mediaPartRepository.SoftDeleteByFilePathAsync(path);

            if (deletedCount > 0)
            {
                LogPathRemoved(this.logger, path, deletedCount);
            }
        }
    }

    private async Task ProcessScansAsync(
        int librarySectionId,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken
    )
    {
        // For now, we trigger a targeted scan for each path
        // Future optimization: batch paths and use a targeted scan API
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Use the scanner service to process individual paths
                // The scanner will handle file resolution, metadata extraction, etc.
                await this.libraryScannerService.ScanPathAsync(
                    librarySectionId,
                    path,
                    cancellationToken
                );
                LogPathScanned(this.logger, path, librarySectionId);
            }
            catch (Exception ex)
            {
                LogPathScanFailed(this.logger, path, ex);
                // Continue with other paths even if one fails
            }
        }
    }
}
