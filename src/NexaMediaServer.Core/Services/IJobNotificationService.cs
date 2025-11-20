// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for monitoring and publishing job notifications.
/// </summary>
public interface IJobNotificationService
{
    /// <summary>
    /// Publishes a job notification to all subscribed clients.
    /// </summary>
    /// <param name="notification">The job notification to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishNotificationAsync(
        JobNotification notification,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the current status of all active jobs.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of active job notifications.</returns>
    Task<IEnumerable<JobNotification>> GetActiveJobsAsync(
        CancellationToken cancellationToken = default
    );
}
