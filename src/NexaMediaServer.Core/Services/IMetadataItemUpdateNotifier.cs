// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Publishes notifications when metadata items are updated so interested parties (e.g., GraphQL subscriptions) can react.
/// </summary>
public interface IMetadataItemUpdateNotifier
{
    /// <summary>
    /// Notify that a metadata item has been updated.
    /// </summary>
    /// <param name="metadataItemUuid">The UUID of the updated metadata item.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyUpdatedAsync(Guid metadataItemUuid, CancellationToken cancellationToken = default);
}
