// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a job notification event sent to clients via GraphQL subscriptions.
/// </summary>
public class JobNotification
{
    /// <summary>
    /// Gets or sets the unique identifier for this job instance.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the type of job.
    /// </summary>
    public required JobType Type { get; set; }

    /// <summary>
    /// Gets or sets the library section ID if the job is related to a specific library.
    /// </summary>
    public int? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library section name if available.
    /// </summary>
    public string? LibrarySectionName { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the job.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the number of completed items.
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the job is still running.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this notification was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
