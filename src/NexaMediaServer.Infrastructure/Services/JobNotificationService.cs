// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Globalization;
using Hangfire;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Implementation of job notification service that monitors Hangfire jobs and publishes notifications.
/// </summary>
public sealed partial class JobNotificationService : IJobNotificationService
{
    private readonly ILibrarySectionRepository libraryRepository;
    private readonly ILibraryScanRepository scanRepository;
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly IJobNotificationPublisher publisher;
    private readonly ILogger<JobNotificationService> logger;
    private readonly ConcurrentDictionary<string, JobNotification> activeJobs = new();

    // Cache for library names to avoid repeated database queries
    private readonly ConcurrentDictionary<int, string> libraryNameCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JobNotificationService"/> class.
    /// </summary>
    /// <param name="libraryRepository">The library repository for retrieving library names.</param>
    /// <param name="scanRepository">The scan repository for retrieving scan status.</param>
    /// <param name="metadataItemRepository">The metadata item repository for counting items.</param>
    /// <param name="publisher">The publisher for sending notifications to clients.</param>
    /// <param name="logger">The logger.</param>
    public JobNotificationService(
        ILibrarySectionRepository libraryRepository,
        ILibraryScanRepository scanRepository,
        IMetadataItemRepository metadataItemRepository,
        IJobNotificationPublisher publisher,
        ILogger<JobNotificationService> logger
    )
    {
        this.libraryRepository = libraryRepository;
        this.scanRepository = scanRepository;
        this.metadataItemRepository = metadataItemRepository;
        this.publisher = publisher;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishNotificationAsync(
        JobNotification notification,
        CancellationToken cancellationToken = default
    )
    {
        if (notification.IsActive)
        {
            this.activeJobs[notification.Id] = notification;
        }
        else
        {
            this.activeJobs.TryRemove(notification.Id, out _);
        }

        await this.publisher.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
        this.LogJobNotificationPublished(
            notification.Id,
            notification.Type,
            notification.ProgressPercentage
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<JobNotification>> GetActiveJobsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var monitoringApi = JobStorage.Current?.GetMonitoringApi();
        if (monitoringApi == null)
        {
            return Enumerable.Empty<JobNotification>();
        }

        // Dictionary to aggregate jobs: key is "JobType:LibrarySectionId", value is list of job IDs
        var jobGroups =
            new Dictionary<
                string,
                (
                    JobType Type,
                    int? LibrarySectionId,
                    List<string> JobIds,
                    DateTime? EarliestTimestamp
                )
            >();

        // Collect all job data first without database queries
        var jobDataList = new List<(string JobId, Job Job, DateTime? Timestamp)>();

        // Get processing jobs
        var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue);
        foreach (var job in processingJobs)
        {
            if (job.Value.Job != null)
            {
                jobDataList.Add((job.Key, job.Value.Job, job.Value.StartedAt));
            }
        }

        // Get enqueued jobs (pending) from all queues
        var queues = monitoringApi.Queues();
        foreach (var queue in queues)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs(queue.Name, 0, int.MaxValue);
            foreach (var job in enqueuedJobs)
            {
                if (job.Value.Job != null)
                {
                    jobDataList.Add((job.Key, job.Value.Job, job.Value.EnqueuedAt));
                }
            }
        }

        // Batch process jobs - extract all IDs first, then query database once
        var scanIds = new HashSet<int>();
        var metadataItemUuids = new HashSet<Guid>();
        var jobTypeMapping = new Dictionary<string, (JobType Type, object? Identifier)>();

        foreach (var (jobId, job, _) in jobDataList)
        {
            var methodName = job.Method.Name;
            var jobType = DetermineJobType(methodName);
            if (jobType == null)
            {
                continue;
            }

            if (jobType == JobType.LibraryScan)
            {
                var scanId = ExtractScanIdFromJob(job);
                if (scanId != null)
                {
                    scanIds.Add(scanId.Value);
                    jobTypeMapping[jobId] = (jobType.Value, scanId.Value);
                }
            }
            else
            {
                var metadataItemUuid = ExtractMetadataItemUuidFromJob(job);
                if (metadataItemUuid != null)
                {
                    metadataItemUuids.Add(metadataItemUuid.Value);
                    jobTypeMapping[jobId] = (jobType.Value, metadataItemUuid.Value);
                }
            }
        }

        // Batch query all scans and metadata items
        var scanToLibraryMap = new Dictionary<int, int>();
        var metadataToLibraryMap = new Dictionary<Guid, int>();

        if (scanIds.Count > 0)
        {
            var scans = await this
                .scanRepository.GetQueryable()
                .Where(s => scanIds.Contains(s.Id))
                .Select(s => new { s.Id, s.LibrarySectionId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var scan in scans)
            {
                scanToLibraryMap[scan.Id] = scan.LibrarySectionId;
            }
        }

        if (metadataItemUuids.Count > 0)
        {
            var metadataItems = await this
                .metadataItemRepository.GetQueryable()
                .Where(m => metadataItemUuids.Contains(m.Uuid))
                .Select(m => new { m.Uuid, m.LibrarySectionId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var item in metadataItems)
            {
                metadataToLibraryMap[item.Uuid] = item.LibrarySectionId;
            }
        }

        // Now group jobs efficiently
        foreach (var (jobId, _, timestamp) in jobDataList)
        {
            if (!jobTypeMapping.TryGetValue(jobId, out var mapping))
            {
                continue;
            }

            var (jobType, identifier) = mapping;
            int? librarySectionId = null;

            if (jobType == JobType.LibraryScan && identifier is int scanId)
            {
                scanToLibraryMap.TryGetValue(scanId, out var libraryId);
                librarySectionId = libraryId != 0 ? libraryId : null;
            }
            else if (identifier is Guid uuid)
            {
                metadataToLibraryMap.TryGetValue(uuid, out var libraryId);
                librarySectionId = libraryId != 0 ? libraryId : null;
            }

            var key = CreateJobKey(jobType, librarySectionId);

            if (!jobGroups.TryGetValue(key, out var existing))
            {
                jobGroups[key] = (jobType, librarySectionId, new List<string> { jobId }, timestamp);
            }
            else
            {
                existing.JobIds.Add(jobId);

                var earliest = existing.EarliestTimestamp;
                if (timestamp != null && (earliest == null || timestamp < earliest))
                {
                    earliest = timestamp;
                }

                jobGroups[key] = (
                    existing.Type,
                    existing.LibrarySectionId,
                    existing.JobIds,
                    earliest
                );
            }
        }

        // Create aggregated notifications
        var notifications = new List<JobNotification>();
        foreach (var kv in jobGroups)
        {
            var key = kv.Key;
            var (type, librarySectionId, jobIds, earliest) = kv.Value;
            var notification = await this.CreateAggregatedNotificationAsync(
                    type,
                    librarySectionId,
                    jobIds,
                    earliest,
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (notification != null)
            {
                notifications.Add(notification);
                this.activeJobs[key] = notification;
            }
        }

        // Remove jobs that are no longer active and send completion notifications
        var activeKeys = new HashSet<string>(jobGroups.Keys);
        var keysToRemove = this.activeJobs.Keys.Where(key => !activeKeys.Contains(key)).ToList();
        foreach (var key in keysToRemove)
        {
            if (this.activeJobs.TryRemove(key, out var completedJob))
            {
                // Send a final notification with IsActive = false to signal completion
                var completionNotification = new JobNotification
                {
                    Id = completedJob.Id,
                    Type = completedJob.Type,
                    LibrarySectionId = completedJob.LibrarySectionId,
                    LibrarySectionName = completedJob.LibrarySectionName,
                    Description = completedJob.Description,
                    IsActive = false,
                    ProgressPercentage = 100.0,
                    CompletedItems = completedJob.TotalItems,
                    TotalItems = completedJob.TotalItems,
                    Timestamp = DateTime.UtcNow,
                };
                notifications.Add(completionNotification);
            }
        }

        return notifications;
    }

    private static string CreateJobKey(JobType jobType, int? librarySectionId)
    {
        return $"{jobType}:{librarySectionId?.ToString(CultureInfo.InvariantCulture) ?? "global"}";
    }

    private static JobType? DetermineJobType(string methodName)
    {
        return methodName switch
        {
            "ExecuteScanAsync" => JobType.LibraryScan,
            "RefreshMetadataAsync" => JobType.MetadataRefresh,
            "AnalyzeFilesAsync" => JobType.FileAnalysis,
            "GenerateImagesAsync" => JobType.ImageGeneration,
            "GenerateTrickplayAsync" => JobType.TrickplayGeneration,
            _ => null,
        };
    }

    private static int? ExtractScanIdFromJob(Job job)
    {
        if (job.Args == null || job.Args.Count == 0)
        {
            return null;
        }

        var firstArg = job.Args[0];
        if (firstArg is int scanId)
        {
            return scanId;
        }

        if (int.TryParse(firstArg?.ToString(), out var parsedId))
        {
            return parsedId;
        }

        return null;
    }

    private static Guid? ExtractMetadataItemUuidFromJob(Job job)
    {
        if (job.Args == null || job.Args.Count == 0)
        {
            return null;
        }

        var firstArg = job.Args[0];
        if (firstArg is Guid uuid)
        {
            return uuid;
        }

        if (Guid.TryParse(firstArg?.ToString(), out var parsedUuid))
        {
            return parsedUuid;
        }

        return null;
    }

    private async Task<string?> GetLibraryNameAsync(int librarySectionId)
    {
        // Check cache first
        if (this.libraryNameCache.TryGetValue(librarySectionId, out var cachedName))
        {
            return cachedName;
        }

        // Query database
        var library = await this
            .libraryRepository.GetByIdAsync(librarySectionId)
            .ConfigureAwait(false);

        if (library?.Name != null)
        {
            // Cache for future use
            this.libraryNameCache.TryAdd(librarySectionId, library.Name);
            return library.Name;
        }

        return null;
    }

    private async Task<JobNotification?> CreateAggregatedNotificationAsync(
        JobType jobType,
        int? librarySectionId,
        List<string> jobIds,
        DateTime? groupStart,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = CreateJobKey(jobType, librarySectionId);

        // For library scans, use the existing detailed tracking
        if (jobType == JobType.LibraryScan && librarySectionId != null)
        {
            // Get the first scan job to extract scan info
            var scan = await this.GetScanForLibraryAsync(librarySectionId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (scan != null)
            {
                var scanLibraryName =
                    await this.GetLibraryNameAsync(scan.LibrarySectionId).ConfigureAwait(false)
                    ?? "Unknown Library";

                var scanTotalItems = scan.TotalFiles;
                var scanProcessedItems = scan.ProcessedFiles;
                var scanProgressPercentage =
                    scanTotalItems > 0 ? (double)scanProcessedItems / scanTotalItems * 100.0 : 0.0;

                return new JobNotification
                {
                    Id = key,
                    Type = JobType.LibraryScan,
                    LibrarySectionId = scan.LibrarySectionId,
                    LibrarySectionName = scanLibraryName,
                    Description = $"Scanning {scanLibraryName}",
                    ProgressPercentage = scanProgressPercentage,
                    CompletedItems = scanProcessedItems,
                    TotalItems = scanTotalItems,
                    IsActive = true,
                    Timestamp = DateTime.UtcNow,
                };
            }
        }

        // For other job types, calculate total items and progress
        string description;
        string? libraryName = null;
        int totalItems = 0;
        int completedItems = 0;

        if (librarySectionId != null)
        {
            libraryName = await this.GetLibraryNameAsync(librarySectionId.Value)
                .ConfigureAwait(false);

            // Use async EF Core queries instead of Task.Run with synchronous LINQ
            totalItems = await this
                .metadataItemRepository.GetQueryable()
                .CountAsync(
                    m => m.LibrarySectionId == librarySectionId.Value && m.ParentId == null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (groupStart != null)
            {
                // Count items that have NOT been updated since the group started
                // These are items still waiting to be processed
                var itemsPending = await this
                    .metadataItemRepository.GetQueryable()
                    .CountAsync(
                        m =>
                            m.LibrarySectionId == librarySectionId.Value
                            && m.ParentId == null
                            && m.UpdatedAt < groupStart.Value,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                // Completed = total - pending
                completedItems = Math.Max(0, totalItems - itemsPending);
            }
            else
            {
                // Fallback heuristic: assume each job in the queue represents an item being processed
                // Since we don't know when the group started, we can't reliably calculate completion
                // Best estimate: assume progress is proportional to queue size
                completedItems = Math.Max(0, totalItems - jobIds.Count);
            }
        }

        var progressPercentage = totalItems > 0 ? (double)completedItems / totalItems * 100.0 : 0.0;

        description = jobType switch
        {
            JobType.MetadataRefresh => libraryName != null
                ? $"Refreshing metadata for {libraryName}"
                : "Refreshing metadata",
            JobType.FileAnalysis => libraryName != null
                ? $"Analyzing files in {libraryName}"
                : "Analyzing files",
            JobType.ImageGeneration => libraryName != null
                ? $"Generating images for {libraryName}"
                : "Generating images",
            JobType.TrickplayGeneration => libraryName != null
                ? $"Generating video previews for {libraryName}"
                : "Generating video previews",
            _ => $"Processing items",
        };

        return new JobNotification
        {
            Id = key,
            Type = jobType,
            LibrarySectionId = librarySectionId,
            LibrarySectionName = libraryName,
            Description = description,
            ProgressPercentage = progressPercentage,
            CompletedItems = completedItems,
            TotalItems = totalItems,
            IsActive = true,
            Timestamp = DateTime.UtcNow,
        };
    }

    private async Task<LibraryScan?> GetScanForLibraryAsync(
        int librarySectionId,
        CancellationToken cancellationToken = default
    )
    {
        // Get the most recent scan for this library using async query
        var scan = await this
            .scanRepository.GetQueryable()
            .Where(s =>
                s.LibrarySectionId == librarySectionId && s.Status == LibraryScanStatus.Running
            )
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return scan;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Published job notification: JobId={JobId}, Type={JobType}, Progress={ProgressPercentage}%"
    )]
    private partial void LogJobNotificationPublished(
        string jobId,
        JobType jobType,
        double progressPercentage
    );
}
