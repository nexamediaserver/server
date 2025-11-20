// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration options for the job notification system.
/// </summary>
public class JobNotificationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "JobNotifications";

    /// <summary>
    /// Gets or sets the interval in milliseconds between notification flushes to subscribers.
    /// Lower values provide more responsive updates but increase message volume.
    /// Default is 500ms.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the number of days to retain completed job notification history.
    /// Entries older than this are purged by the cleanup job.
    /// Default is 7 days.
    /// </summary>
    public int HistoryRetentionDays { get; set; } = 7;
}
