// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of background job being executed.
/// </summary>
public enum JobType
{
    /// <summary>
    /// Library scanning job.
    /// </summary>
    LibraryScan,

    /// <summary>
    /// Metadata refresh job for one or more items.
    /// </summary>
    MetadataRefresh,

    /// <summary>
    /// File analysis job.
    /// </summary>
    FileAnalysis,

    /// <summary>
    /// Image generation job.
    /// </summary>
    ImageGeneration,

    /// <summary>
    /// Trickplay (BIF) generation job.
    /// </summary>
    TrickplayGeneration,

    /// <summary>
    /// Search index rebuild job.
    /// </summary>
    SearchIndexRebuild,
}
