// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Subscriptions;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Services;

/// <summary>
/// GraphQL-based job notification publisher using HotChocolate's topic system.
/// </summary>
internal sealed class GraphQLJobNotificationPublisher : IJobNotificationPublisher
{
    /// <summary>
    /// Gets the topic name used for job notification events.
    /// </summary>
    public const string Topic = "JOB_NOTIFICATION";

    private readonly ITopicEventSender sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLJobNotificationPublisher"/> class.
    /// </summary>
    /// <param name="sender">The topic event sender.</param>
    public GraphQLJobNotificationPublisher(ITopicEventSender sender) => this.sender = sender;

    /// <inheritdoc />
    public async Task PublishAsync(
        JobNotification notification,
        CancellationToken cancellationToken = default
    )
    {
        await this.sender.SendAsync(Topic, notification, cancellationToken).ConfigureAwait(false);
    }
}
