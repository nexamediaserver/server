// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Orchestrates the complete metadata refresh pipeline including local metadata extraction,
/// agent queries, image provider execution, credit upsert, and follow-up job scheduling.
/// </summary>
public interface IMetadataRefreshOrchestrator
{
    /// <summary>
    /// Hangfire job: enrich metadata for a single metadata item using all available sources concurrently.
    /// Runs ISidecarParser, IMetadataAgent, and IImageProvider stages in parallel, then merges
    /// results according to precedence and persists once.
    /// Executes on the "metadata_agents" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the metadata item.</param>
    /// <param name="skipAnalysis">When <see langword="true"/>, skips enqueueing file analysis and trickplay generation jobs.
    /// Use for metadata-only refresh without triggering full media processing.</param>
    /// <param name="overrideFields">Optional collection of field names to force update, bypassing any locks.
    /// Use constants from <see cref="Constants.MetadataFieldNames"/> for built-in fields.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshMetadataAsync(
        Guid metadataItemUuid,
        bool skipAnalysis = false,
        IEnumerable<string>? overrideFields = null);
}
