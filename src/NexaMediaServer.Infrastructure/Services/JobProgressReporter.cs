// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Singleton service that tracks job progress and manages persistent notification entries.
/// Aggregates job progress by (LibrarySectionId, JobType) and queues changes for batch publishing.
/// </summary>
public sealed partial class JobProgressReporter : IJobProgressReporter
{
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ILogger<JobProgressReporter> logger;

    // In-memory buffer of changes pending publish (key: LibrarySectionId_JobType)
    private readonly ConcurrentDictionary<string, JobNotificationEntry> pendingChanges = new();

    // Cache of library names for notification descriptions
    private readonly ConcurrentDictionary<int, string> libraryNameCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JobProgressReporter"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating DbContext instances.</param>
    /// <param name="logger">Logger instance.</param>
    public JobProgressReporter(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ILogger<JobProgressReporter> logger
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(
        int librarySectionId,
        JobType jobType,
        int totalItems,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await GetOrCreateEntryAsync(db, librarySectionId, jobType, cancellationToken);

        entry.Status = JobNotificationStatus.Running;
        entry.TotalItems = totalItems;
        entry.CompletedItems = 0;
        entry.Progress = 0;
        entry.StartedAt = now;
        entry.UpdatedAt = now;
        entry.CompletedAt = null;
        entry.ErrorMessage = null;

        await db.SaveChangesAsync(cancellationToken);

        this.QueueChange(entry);
        this.LogJobStarted(librarySectionId, jobType, totalItems);
    }

    /// <inheritdoc />
    public async Task ReportProgressAsync(
        int librarySectionId,
        JobType jobType,
        int completedItems,
        int totalItems,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await GetOrCreateEntryAsync(db, librarySectionId, jobType, cancellationToken);

        entry.CompletedItems = completedItems;
        entry.TotalItems = totalItems;
        entry.Progress = totalItems > 0 ? (int)((double)completedItems / totalItems * 100) : 0;
        entry.UpdatedAt = DateTime.UtcNow;

        // Ensure status is Running if we're reporting progress
        if (entry.Status == JobNotificationStatus.Pending)
        {
            entry.Status = JobNotificationStatus.Running;
        }

        await db.SaveChangesAsync(cancellationToken);

        this.QueueChange(entry);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(
        int librarySectionId,
        JobType jobType,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await GetOrCreateEntryAsync(db, librarySectionId, jobType, cancellationToken);

        entry.Status = JobNotificationStatus.Completed;
        entry.Progress = 100;
        entry.CompletedItems = entry.TotalItems;
        entry.CompletedAt = now;
        entry.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        this.QueueChange(entry);
        this.LogJobCompleted(librarySectionId, jobType, entry.TotalItems);
    }

    /// <inheritdoc />
    public async Task FailAsync(
        int librarySectionId,
        JobType jobType,
        string errorMessage,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await GetOrCreateEntryAsync(db, librarySectionId, jobType, cancellationToken);

        entry.Status = JobNotificationStatus.Failed;
        entry.CompletedAt = now;
        entry.UpdatedAt = now;
        entry.ErrorMessage = errorMessage.Length > 4096 ? errorMessage[..4096] : errorMessage;

        await db.SaveChangesAsync(cancellationToken);

        this.QueueChange(entry);
        this.LogJobFailed(librarySectionId, jobType, errorMessage);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<JobNotificationEntry> DrainPendingChanges()
    {
        var changes = new List<JobNotificationEntry>();

        foreach (var key in this.pendingChanges.Keys.ToList())
        {
            if (this.pendingChanges.TryRemove(key, out var entry))
            {
                changes.Add(entry);
            }
        }

        return changes;
    }

    /// <inheritdoc />
    public async Task<int> PurgeHistoryAsync(
        int retentionDays,
        CancellationToken cancellationToken = default
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var count = await db
            .JobNotificationEntries.Where(e =>
                e.CompletedAt != null
                && e.CompletedAt < cutoff
                && (
                    e.Status == JobNotificationStatus.Completed
                    || e.Status == JobNotificationStatus.Failed
                )
            )
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
        {
            this.LogHistoryPurged(count, retentionDays);
        }

        return count;
    }

    /// <summary>
    /// Gets the library name for a given library section ID, with caching.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The library name, or null if not found.</returns>
    public async Task<string?> GetLibraryNameAsync(
        int librarySectionId,
        CancellationToken cancellationToken = default
    )
    {
        if (this.libraryNameCache.TryGetValue(librarySectionId, out var name))
        {
            return name;
        }

        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var libraryName = await db
            .LibrarySections.Where(l => l.Id == librarySectionId)
            .Select(l => l.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (libraryName != null)
        {
            this.libraryNameCache.TryAdd(librarySectionId, libraryName);
        }

        return libraryName;
    }

    private static string GetKey(int librarySectionId, JobType jobType) =>
        $"{librarySectionId}_{jobType}";

    private static async Task<JobNotificationEntry> GetOrCreateEntryAsync(
        MediaServerContext db,
        int librarySectionId,
        JobType jobType,
        CancellationToken cancellationToken
    )
    {
        var entry = await db
            .JobNotificationEntries.Where(e =>
                e.LibrarySectionId == librarySectionId && e.JobType == jobType
            )
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
        {
            entry = new JobNotificationEntry
            {
                LibrarySectionId = librarySectionId,
                JobType = jobType,
                Status = JobNotificationStatus.Pending,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.JobNotificationEntries.Add(entry);
        }

        return entry;
    }

    private void QueueChange(JobNotificationEntry entry)
    {
        var key = GetKey(entry.LibrarySectionId, entry.JobType);

        // Create a detached copy for the pending changes buffer
        var copy = new JobNotificationEntry
        {
            Id = entry.Id,
            LibrarySectionId = entry.LibrarySectionId,
            JobType = entry.JobType,
            Status = entry.Status,
            Progress = entry.Progress,
            CompletedItems = entry.CompletedItems,
            TotalItems = entry.TotalItems,
            StartedAt = entry.StartedAt,
            CompletedAt = entry.CompletedAt,
            UpdatedAt = entry.UpdatedAt,
            ErrorMessage = entry.ErrorMessage,
        };

        this.pendingChanges.AddOrUpdate(key, copy, (_, _) => copy);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job started: Library={LibrarySectionId}, Type={JobType}, TotalItems={TotalItems}"
    )]
    private partial void LogJobStarted(int librarySectionId, JobType jobType, int totalItems);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job completed: Library={LibrarySectionId}, Type={JobType}, TotalItems={TotalItems}"
    )]
    private partial void LogJobCompleted(int librarySectionId, JobType jobType, int totalItems);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Job failed: Library={LibrarySectionId}, Type={JobType}, Error={ErrorMessage}"
    )]
    private partial void LogJobFailed(int librarySectionId, JobType jobType, string errorMessage);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Purged {Count} job notification entries older than {RetentionDays} days"
    )]
    private partial void LogHistoryPurged(int count, int retentionDays);
}
