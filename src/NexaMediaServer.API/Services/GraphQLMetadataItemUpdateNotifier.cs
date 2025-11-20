// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Subscriptions;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Services;

/// <summary>
/// GraphQL-based notifier that publishes metadata item update events to HotChocolate's topic system.
/// </summary>
/// <summary>
/// Implementation of <see cref="IMetadataItemUpdateNotifier"/> that uses HotChocolate's <see cref="ITopicEventSender"/> to publish update events.
/// </summary>
internal sealed class GraphQLMetadataItemUpdateNotifier : IMetadataItemUpdateNotifier
{
    /// <summary>
    /// Gets the topic name used for metadata item update events.
    /// </summary>
    public const string Topic = "METADATA_ITEM_UPDATED";

    private readonly ITopicEventSender sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLMetadataItemUpdateNotifier"/> class.
    /// </summary>
    /// <param name="sender">The topic event sender.</param>
    public GraphQLMetadataItemUpdateNotifier(ITopicEventSender sender) => this.sender = sender;

    /// <inheritdoc />
    public async Task NotifyUpdatedAsync(
        Guid metadataItemUuid,
        CancellationToken cancellationToken = default
    )
    {
        // Publish the UUID; the subscription resolver will map it to API type.
        await this
            .sender.SendAsync(Topic, metadataItemUuid, cancellationToken)
            .ConfigureAwait(false);
    }
}
