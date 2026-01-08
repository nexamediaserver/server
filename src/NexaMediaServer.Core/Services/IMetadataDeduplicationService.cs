// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for deduplicating metadata items based on external identifiers.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a generic mechanism to find existing metadata items by their
/// external identifiers (MusicBrainz IDs, ISRC, barcode, etc.) to prevent duplicates
/// during scanning and metadata ingestion.
/// </para>
/// <para>
/// When an external ID match is found, the existing item is returned. Otherwise,
/// the factory function is invoked to create a new item which is then tracked.
/// </para>
/// </remarks>
public interface IMetadataDeduplicationService
{
    /// <summary>
    /// Finds an existing metadata item by external identifier or creates a new one.
    /// </summary>
    /// <param name="metadataType">The type of metadata item to find or create.</param>
    /// <param name="provider">The external ID provider (e.g., "musicbrainz_work", "isrc").</param>
    /// <param name="externalId">The external identifier value.</param>
    /// <param name="librarySectionId">The library section to search within.</param>
    /// <param name="factory">Factory function to create a new item if not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created metadata item.</returns>
    Task<MetadataItem> FindOrCreateByExternalIdAsync(
        MetadataType metadataType,
        string provider,
        string externalId,
        int librarySectionId,
        Func<MetadataItem> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing metadata item by any of the provided external identifiers.
    /// </summary>
    /// <param name="metadataType">The type of metadata item to find or create.</param>
    /// <param name="externalIds">Dictionary of provider → external ID pairs to search for.</param>
    /// <param name="librarySectionId">The library section to search within.</param>
    /// <param name="factory">Factory function to create a new item if not found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created metadata item.</returns>
    Task<MetadataItem> FindOrCreateByExternalIdsAsync(
        MetadataType metadataType,
        IReadOnlyDictionary<string, string> externalIds,
        int librarySectionId,
        Func<MetadataItem> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing metadata item by external identifier without creating.
    /// </summary>
    /// <param name="metadataType">The type of metadata item to find.</param>
    /// <param name="provider">The external ID provider.</param>
    /// <param name="externalId">The external identifier value.</param>
    /// <param name="librarySectionId">The library section to search within, or null for global search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing metadata item, or null if not found.</returns>
    Task<MetadataItem?> FindByExternalIdAsync(
        MetadataType metadataType,
        string provider,
        string externalId,
        int? librarySectionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an external identifier for an existing metadata item.
    /// </summary>
    /// <param name="metadataItem">The metadata item to register the identifier for.</param>
    /// <param name="provider">The external ID provider.</param>
    /// <param name="externalId">The external identifier value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterExternalIdAsync(
        MetadataItem metadataItem,
        string provider,
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the in-memory cache of tracked items.
    /// </summary>
    /// <remarks>
    /// Should be called at the end of a scan batch to release memory.
    /// </remarks>
    void ClearCache();
}
