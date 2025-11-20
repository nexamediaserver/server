// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Background service that periodically flushes pending job notification changes to GraphQL subscribers.
/// </summary>
public sealed partial class JobNotificationFlushService : BackgroundService
{
    private readonly IJobProgressReporter progressReporter;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<JobNotificationFlushService> logger;
    private readonly JobNotificationOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobNotificationFlushService"/> class.
    /// </summary>
    /// <param name="progressReporter">The job progress reporter.</param>
    /// <param name="scopeFactory">The service scope factory for resolving scoped services.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Configuration options.</param>
    public JobNotificationFlushService(
        IJobProgressReporter progressReporter,
        IServiceScopeFactory scopeFactory,
        ILogger<JobNotificationFlushService> logger,
        IOptions<JobNotificationOptions> options
    )
    {
        this.progressReporter = progressReporter;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.LogFlushServiceStarted(this.options.FlushIntervalMs);

        var flushInterval = TimeSpan.FromMilliseconds(this.options.FlushIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingChanges = this.progressReporter.DrainPendingChanges();

                if (pendingChanges.Count > 0)
                {
                    using var scope = this.scopeFactory.CreateScope();
                    var publisher =
                        scope.ServiceProvider.GetRequiredService<IJobNotificationPublisher>();

                    foreach (var entry in pendingChanges)
                    {
                        var notification = await this.CreateNotificationAsync(entry, stoppingToken);
                        await publisher.PublishAsync(notification, stoppingToken);
                    }

                    this.LogFlushedNotifications(pendingChanges.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this.LogFlushError(ex);
            }

            await Task.Delay(flushInterval, stoppingToken);
        }

        this.LogFlushServiceStopped();
    }

    private static string GetDescription(JobType jobType, string? libraryName)
    {
        var libraryPart = string.IsNullOrEmpty(libraryName) ? string.Empty : $" for {libraryName}";
        return jobType switch
        {
            JobType.LibraryScan => $"Scanning library{libraryPart}",
            JobType.MetadataRefresh => $"Refreshing metadata{libraryPart}",
            JobType.FileAnalysis => $"Analyzing files{libraryPart}",
            JobType.ImageGeneration => $"Generating images{libraryPart}",
            JobType.TrickplayGeneration => $"Generating trickplay{libraryPart}",
            _ => $"Processing{libraryPart}",
        };
    }

    private async Task<JobNotification> CreateNotificationAsync(
        Core.Entities.JobNotificationEntry entry,
        CancellationToken cancellationToken
    )
    {
        // Get library name from the reporter's cache
        var libraryName = await ((JobProgressReporter)this.progressReporter).GetLibraryNameAsync(
            entry.LibrarySectionId,
            cancellationToken
        );

        var isActive =
            entry.Status is JobNotificationStatus.Pending or JobNotificationStatus.Running;

        return new JobNotification
        {
            Id = $"{entry.LibrarySectionId}_{entry.JobType}",
            Type = entry.JobType,
            LibrarySectionId = entry.LibrarySectionId,
            LibrarySectionName = libraryName,
            Description = GetDescription(entry.JobType, libraryName),
            ProgressPercentage = entry.Progress,
            CompletedItems = entry.CompletedItems,
            TotalItems = entry.TotalItems,
            IsActive = isActive,
            Timestamp = entry.UpdatedAt,
        };
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job notification flush service started with interval {IntervalMs}ms"
    )]
    private partial void LogFlushServiceStarted(int intervalMs);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job notification flush service stopped"
    )]
    private partial void LogFlushServiceStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Flushed {Count} job notifications")]
    private partial void LogFlushedNotifications(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error flushing job notifications")]
    private partial void LogFlushError(Exception exception);
}
