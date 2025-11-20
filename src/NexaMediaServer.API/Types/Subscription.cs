// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate;
using HotChocolate.Authorization;
using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.API.Services;
using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL subscription operations for the API.
/// </summary>
[SubscriptionType]
public static class Subscription
{
    /// <summary>
    /// Streams metadata items as they are updated. Clients receive the full mapped metadata item.
    /// </summary>
    /// <param name="metadataItemUuid">The UUID of the updated metadata item.</param>
    /// <param name="dataLoader">Metadata item dataloader.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated metadata item mapped to the API type, or null if not found.</returns>
    [Subscribe]
    [Topic(GraphQLMetadataItemUpdateNotifier.Topic)]
    [Authorize]
    public static async Task<MetadataItem?> OnMetadataItemUpdatedAsync(
        [EventMessage] Guid metadataItemUuid,
        IMetadataItemByIdDataLoader dataLoader,
        CancellationToken cancellationToken
    )
    {
        return await dataLoader.LoadAsync(metadataItemUuid, cancellationToken);
    }

    /// <summary>
    /// Streams job notifications for background tasks such as library scans and metadata refresh.
    /// Clients receive real-time updates about job progress and completion.
    /// </summary>
    /// <param name="notification">The job notification event.</param>
    /// <returns>The job notification with progress information.</returns>
    [Subscribe]
    [Topic(GraphQLJobNotificationPublisher.Topic)]
    [Authorize]
    public static JobNotification OnJobNotification([EventMessage] JobNotification notification) =>
        notification;
}
