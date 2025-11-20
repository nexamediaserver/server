// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Interface for publishing job notifications to clients.
/// </summary>
public interface IJobNotificationPublisher
{
    /// <summary>
    /// Publishes a job notification to subscribed clients.
    /// </summary>
    /// <param name="notification">The job notification to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(JobNotification notification, CancellationToken cancellationToken = default);
}
