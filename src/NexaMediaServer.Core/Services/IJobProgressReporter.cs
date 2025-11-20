// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Reports job progress for the unified notification system.
/// Jobs call these methods to update their progress, which is aggregated by (LibraryId, JobType)
/// and published to subscribers.
/// </summary>
public interface IJobProgressReporter
{
    /// <summary>
    /// Starts tracking a job for the specified library and job type.
    /// Creates or updates the job notification entry with Pending/Running status.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="jobType">The type of job.</param>
    /// <param name="totalItems">The total number of items to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(
        int librarySectionId,
        JobType jobType,
        int totalItems,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reports progress for an ongoing job.
    /// Updates the job notification entry with the current progress.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="jobType">The type of job.</param>
    /// <param name="completedItems">The number of items completed.</param>
    /// <param name="totalItems">The total number of items to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReportProgressAsync(
        int librarySectionId,
        JobType jobType,
        int completedItems,
        int totalItems,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks a job as successfully completed.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="jobType">The type of job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteAsync(
        int librarySectionId,
        JobType jobType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks a job as failed with an error message.
    /// </summary>
    /// <param name="librarySectionId">The library section ID.</param>
    /// <param name="jobType">The type of job.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FailAsync(
        int librarySectionId,
        JobType jobType,
        string errorMessage,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all pending changes that need to be published to subscribers.
    /// This is called by the flush service to batch notifications.
    /// </summary>
    /// <returns>A collection of job notification entries that have changed since the last flush.</returns>
    IReadOnlyCollection<Core.Entities.JobNotificationEntry> DrainPendingChanges();

    /// <summary>
    /// Purges completed job notification entries older than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain completed entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entries purged.</returns>
    Task<int> PurgeHistoryAsync(int retentionDays, CancellationToken cancellationToken = default);
}
