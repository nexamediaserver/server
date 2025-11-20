// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service responsible for orchestrating file analysis (FFprobe, MediaInfo)
/// and merging technical metadata into media items.
/// </summary>
public interface IFileAnalysisOrchestrator
{
    /// <summary>
    /// Hangfire job: analyze media files for a metadata item and merge technical characteristics.
    /// Executes on the "file_analyzers" queue.
    /// </summary>
    /// <param name="metadataItemUuid">UUID of the parent metadata item.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AnalyzeFilesAsync(Guid metadataItemUuid);
}
