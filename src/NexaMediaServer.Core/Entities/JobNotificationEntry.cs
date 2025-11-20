// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a persisted job notification entry for tracking background job progress.
/// Aggregated by (LibrarySectionId, JobType) to provide unified progress tracking.
/// </summary>
public class JobNotificationEntry : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the library this job is operating on.
    /// </summary>
    public int LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library entity this job is operating on.
    /// </summary>
    public LibrarySection LibrarySection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of job being tracked.
    /// </summary>
    public JobType JobType { get; set; }

    /// <summary>
    /// Gets or sets the current status of the job.
    /// </summary>
    public JobNotificationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Gets or sets the number of items completed.
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job completed, or null if still in progress.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entry was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets an error message if the job failed, or null if successful.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
