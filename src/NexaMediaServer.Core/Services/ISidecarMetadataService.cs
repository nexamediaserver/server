// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service responsible for parsing sidecar files (.nfo, metadata.json)
/// and extracting embedded metadata from media files.
/// </summary>
public interface ISidecarMetadataService
{
    /// <summary>
    /// Extracts local metadata from sidecar files and embedded tags for a metadata item.
    /// Applies overlay to item fields and collects credits for later processing.
    /// </summary>
    /// <param name="item">The metadata item to enrich.</param>
    /// <param name="library">The library section containing the item.</param>
    /// <param name="overrideFields">Optional collection of field names to force update, bypassing any locks.
    /// Use constants from <see cref="Constants.MetadataFieldNames"/> for built-in fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing applied status and extracted credits.</returns>
    Task<SidecarEnrichmentResult> ExtractLocalMetadataAsync(
        MetadataItem item,
        LibrarySection library,
        IEnumerable<string>? overrideFields = null,
        CancellationToken cancellationToken = default
    );
}
