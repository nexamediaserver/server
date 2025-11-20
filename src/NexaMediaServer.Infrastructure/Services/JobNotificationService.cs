// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
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
    private static readonly HashSet<string> MonitoredQueues = new(StringComparer.OrdinalIgnoreCase)
    {
        "scans",
        "metadata_agents",
        "file_analyzers",
        "image_generators",
        "trickplay",
    };

    private readonly ILibrarySectionRepository libraryRepository;
    private readonly ILibraryScanRepository scanRepository;
    private readonly IMetadataItemRepository metadataItemRepository;
    private readonly IJobNotificationPublisher publisher;
    private readonly ILogger<JobNotificationService> logger;
    private readonly ConcurrentDictionary<string, JobNotification> publishedJobs = new();
    private readonly ConcurrentDictionary<string, JobNotification> aggregatedJobs = new();

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
            this.publishedJobs[notification.Id] = notification;
        }
        else
        {
            this.publishedJobs.TryRemove(notification.Id, out _);
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
            return this.CreateCompletionNotifications(new HashSet<string>(StringComparer.Ordinal));
        }

        var jobDataList = new List<(string JobId, Job Job, DateTime? Timestamp)>();
        CollectProcessingJobs(monitoringApi, jobDataList);
        CollectEnqueuedJobs(monitoringApi, jobDataList);

        if (jobDataList.Count == 0)
        {
            return this.CreateCompletionNotifications(new HashSet<string>(StringComparer.Ordinal));
        }

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
            var normalizedTimestamp = NormalizeTimestamp(timestamp);

            if (!jobGroups.TryGetValue(key, out var existing))
            {
                jobGroups[key] = (
                    jobType,
                    librarySectionId,
                    new List<string> { jobId },
                    normalizedTimestamp
                );
            }
            else
            {
                existing.JobIds.Add(jobId);

                var earliest = existing.EarliestTimestamp;
                if (
                    normalizedTimestamp != null
                    && (earliest == null || normalizedTimestamp < earliest)
                )
                {
                    earliest = normalizedTimestamp;
                }

                jobGroups[key] = (
                    existing.Type,
                    existing.LibrarySectionId,
                    existing.JobIds,
                    earliest
                );
            }
        }

        var notifications = new List<JobNotification>();
        var activeKeys = new HashSet<string>(StringComparer.Ordinal);

        var librarySectionIds = jobGroups
            .Values.Select(v => v.LibrarySectionId)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToArray();

        await this.EnsureLibraryNamesCachedAsync(librarySectionIds, cancellationToken)
            .ConfigureAwait(false);

        var scanLibraryIds = jobGroups
            .Values.Where(v => v.Type == JobType.LibraryScan && v.LibrarySectionId.HasValue)
            .Select(v => v.LibrarySectionId!.Value)
            .Distinct()
            .ToArray();

        var runningScans = await this.LoadRunningScansAsync(scanLibraryIds, cancellationToken)
            .ConfigureAwait(false);

        var nonScanLibraryIds = jobGroups
            .Values.Where(v => v.Type != JobType.LibraryScan && v.LibrarySectionId.HasValue)
            .Select(v => v.LibrarySectionId!.Value)
            .Distinct()
            .ToArray();

        var libraryTotals = await this.LoadLibraryTotalsAsync(nonScanLibraryIds, cancellationToken)
            .ConfigureAwait(false);

        var pendingItemsCache = new Dictionary<(int LibraryId, long TimestampTicks), int>();

        foreach (var (key, (type, librarySectionId, jobIds, earliest)) in jobGroups)
        {
            var notification = await this.CreateAggregatedNotificationAsync(
                    key,
                    type,
                    librarySectionId,
                    jobIds,
                    earliest,
                    runningScans,
                    libraryTotals,
                    pendingItemsCache,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (notification == null)
            {
                continue;
            }

            notifications.Add(notification);

            if (notification.IsActive)
            {
                activeKeys.Add(notification.Id);
                this.aggregatedJobs[notification.Id] = notification;
            }
        }

        notifications.AddRange(this.CreateCompletionNotifications(activeKeys));
        return notifications;

        static void CollectProcessingJobs(
            Hangfire.Storage.IMonitoringApi monitoringApi,
            List<(string JobId, Job Job, DateTime? Timestamp)> destination
        )
        {
            const int pageSize = 200;
            var offset = 0;

            while (true)
            {
                var processingJobs = monitoringApi.ProcessingJobs(offset, pageSize);
                if (processingJobs.Count == 0)
                {
                    break;
                }

                foreach (var job in processingJobs)
                {
                    if (job.Value.Job == null)
                    {
                        continue;
                    }

                    destination.Add(
                        (job.Key, job.Value.Job, NormalizeTimestamp(job.Value.StartedAt))
                    );
                }

                if (processingJobs.Count < pageSize)
                {
                    break;
                }

                offset += pageSize;
            }
        }

        static void CollectEnqueuedJobs(
            Hangfire.Storage.IMonitoringApi monitoringApi,
            List<(string JobId, Job Job, DateTime? Timestamp)> destination
        )
        {
            const int pageSize = 500;

            var queues = monitoringApi.Queues();
            foreach (var queue in queues)
            {
                if (!MonitoredQueues.Contains(queue.Name))
                {
                    continue;
                }

                var queueLength = ClampCount(queue.Length);
                if (queueLength == 0)
                {
                    continue;
                }

                for (var offset = 0; offset < queueLength; offset += pageSize)
                {
                    var take = Math.Min(pageSize, queueLength - offset);
                    var enqueuedJobs = monitoringApi.EnqueuedJobs(queue.Name, offset, take);

                    if (enqueuedJobs.Count == 0)
                    {
                        break;
                    }

                    foreach (var job in enqueuedJobs)
                    {
                        if (job.Value.Job == null)
                        {
                            continue;
                        }

                        destination.Add(
                            (job.Key, job.Value.Job, NormalizeTimestamp(job.Value.EnqueuedAt))
                        );
                    }

                    if (enqueuedJobs.Count < take)
                    {
                        break;
                    }
                }
            }
        }

        static int ClampCount(long value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return value > int.MaxValue ? int.MaxValue : (int)value;
        }
    }

    private static DateTime? NormalizeTimestamp(DateTime? timestamp)
    {
        if (timestamp == null)
        {
            return null;
        }

        var value = timestamp.Value;
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
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
        string jobKey,
        JobType jobType,
        int? librarySectionId,
        List<string> jobIds,
        DateTime? groupStart,
        IReadOnlyDictionary<int, LibraryScan> runningScans,
        IReadOnlyDictionary<int, int> libraryTotals,
        Dictionary<(int LibraryId, long TimestampTicks), int> pendingItemsCache,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For library scans, use the existing detailed tracking
        if (jobType == JobType.LibraryScan && librarySectionId != null)
        {
            var libraryId = librarySectionId.Value;
            if (!runningScans.TryGetValue(libraryId, out var scan))
            {
                scan = await this.GetScanForLibraryAsync(libraryId, cancellationToken)
                    .ConfigureAwait(false);
            }

            var scanLibraryName =
                await this.GetLibraryNameAsync(libraryId).ConfigureAwait(false)
                ?? "Unknown Library";

            if (scan != null)
            {
                var scanTotalItems = scan.TotalFiles;
                var scanProcessedItems = scan.ProcessedFiles;
                var scanProgressPercentage =
                    scanTotalItems > 0 ? (double)scanProcessedItems / scanTotalItems * 100.0 : 0.0;

                return new JobNotification
                {
                    Id = jobKey,
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

            return new JobNotification
            {
                Id = jobKey,
                Type = JobType.LibraryScan,
                LibrarySectionId = libraryId,
                LibrarySectionName = scanLibraryName,
                Description = $"Scanning {scanLibraryName}",
                ProgressPercentage = 0.0,
                CompletedItems = 0,
                TotalItems = 0,
                IsActive = true,
                Timestamp = DateTime.UtcNow,
            };
        }

        // For other job types, calculate total items and progress
        string description;
        string? libraryName = null;
        int totalItems = 0;
        int completedItems = 0;

        if (librarySectionId != null)
        {
            libraryName = this.libraryNameCache.TryGetValue(
                librarySectionId.Value,
                out var cachedName
            )
                ? cachedName
                : await this.GetLibraryNameAsync(librarySectionId.Value).ConfigureAwait(false);

            if (libraryTotals.TryGetValue(librarySectionId.Value, out var cachedTotal))
            {
                totalItems = cachedTotal;
            }

            if (totalItems > 0)
            {
                if (groupStart != null)
                {
                    var normalizedCutoff = NormalizeTimestamp(groupStart) ?? DateTime.UtcNow;
                    var pendingItems = await this.GetPendingItemCountAsync(
                            librarySectionId.Value,
                            normalizedCutoff,
                            pendingItemsCache,
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    completedItems = Math.Max(0, totalItems - pendingItems);
                }
                else
                {
                    completedItems = Math.Max(0, totalItems - jobIds.Count);
                }
            }
        }
        else
        {
            totalItems = jobIds.Count;
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
            Id = jobKey,
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

    private IEnumerable<JobNotification> CreateCompletionNotifications(HashSet<string> activeKeys)
    {
        foreach (var key in this.aggregatedJobs.Keys)
        {
            if (activeKeys.Contains(key))
            {
                continue;
            }

            if (!this.aggregatedJobs.TryRemove(key, out var completedJob))
            {
                continue;
            }

            yield return new JobNotification
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
        }
    }

    private async Task EnsureLibraryNamesCachedAsync(
        int[] librarySectionIds,
        CancellationToken cancellationToken
    )
    {
        if (librarySectionIds.Length == 0)
        {
            return;
        }

        var missingIds = librarySectionIds
            .Where(id => !this.libraryNameCache.ContainsKey(id))
            .Distinct()
            .ToArray();

        if (missingIds.Length == 0)
        {
            return;
        }

        var libraries = await this
            .libraryRepository.GetQueryable()
            .Where(l => missingIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var library in libraries)
        {
            this.libraryNameCache.TryAdd(library.Id, library.Name);
        }
    }

    private async Task<IReadOnlyDictionary<int, LibraryScan>> LoadRunningScansAsync(
        int[] librarySectionIds,
        CancellationToken cancellationToken
    )
    {
        if (librarySectionIds.Length == 0)
        {
            return new Dictionary<int, LibraryScan>();
        }

        var scans = await this
            .scanRepository.GetQueryable()
            .Where(s =>
                librarySectionIds.Contains(s.LibrarySectionId)
                && s.Status == LibraryScanStatus.Running
            )
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return scans
            .GroupBy(s => s.LibrarySectionId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<IReadOnlyDictionary<int, int>> LoadLibraryTotalsAsync(
        int[] librarySectionIds,
        CancellationToken cancellationToken
    )
    {
        if (librarySectionIds.Length == 0)
        {
            return new Dictionary<int, int>();
        }

        var totals = await this
            .metadataItemRepository.GetQueryable()
            .Where(m => m.ParentId == null && librarySectionIds.Contains(m.LibrarySectionId))
            .GroupBy(m => m.LibrarySectionId)
            .Select(group => new { LibrarySectionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(
                group => group.LibrarySectionId,
                group => group.Count,
                cancellationToken
            )
            .ConfigureAwait(false);

        return totals;
    }

    private async Task<int> GetPendingItemCountAsync(
        int librarySectionId,
        DateTime cutoff,
        Dictionary<(int LibraryId, long TimestampTicks), int> cache,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = (librarySectionId, cutoff.Ticks);
        if (cache.TryGetValue(cacheKey, out var cachedValue))
        {
            return cachedValue;
        }

        var pending = await this
            .metadataItemRepository.GetQueryable()
            .CountAsync(
                m =>
                    m.LibrarySectionId == librarySectionId
                    && m.ParentId == null
                    && m.UpdatedAt < cutoff,
                cancellationToken
            )
            .ConfigureAwait(false);

        cache[cacheKey] = pending;
        return pending;
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
