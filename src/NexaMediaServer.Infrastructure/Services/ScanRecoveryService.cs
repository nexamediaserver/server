// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Background service that detects and resumes interrupted library scans on startup.
/// </summary>
public sealed partial class ScanRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ScanRecoveryService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanRecoveryService"/> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory for creating scopes.</param>
    /// <param name="logger">The logger.</param>
    public ScanRecoveryService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ScanRecoveryService> logger
    )
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give other services time to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        try
        {
            await using var scope = this.serviceScopeFactory.CreateAsyncScope();
            var scannerService = scope.ServiceProvider.GetRequiredService<ILibraryScannerService>();

            var interruptedScans = await scannerService
                .GetInterruptedScansAsync()
                .ConfigureAwait(false);

            if (interruptedScans.Count == 0)
            {
                LogNoInterruptedScans(this.logger);
                return;
            }

            LogFoundInterruptedScans(this.logger, interruptedScans.Count);

            foreach (var scan in interruptedScans)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                LogResumingScan(this.logger, scan.Id, scan.LibrarySectionId);

                try
                {
                    await scannerService.ResumeScanAsync(scan.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogResumeError(this.logger, scan.Id, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }
        catch (Exception ex)
        {
            LogRecoveryError(this.logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No interrupted scans found on startup")]
    private static partial void LogNoInterruptedScans(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {Count} interrupted scan(s) to resume"
    )]
    private static partial void LogFoundInterruptedScans(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Resuming interrupted scan {ScanId} for library {LibraryId}"
    )]
    private static partial void LogResumingScan(ILogger logger, int scanId, int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resume scan {ScanId}")]
    private static partial void LogResumeError(ILogger logger, int scanId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during scan recovery")]
    private static partial void LogRecoveryError(ILogger logger, Exception ex);
}
