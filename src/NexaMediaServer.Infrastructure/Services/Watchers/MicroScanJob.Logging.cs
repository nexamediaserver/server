// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Logger message definitions for <see cref="MicroScanJob"/>.
/// </summary>
public sealed partial class MicroScanJob
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting micro-scan for library {LibraryId}: {ScanCount} paths to scan, {RemoveCount} paths to remove"
    )]
    private static partial void LogMicroScanStarted(
        ILogger logger,
        int libraryId,
        int scanCount,
        int removeCount
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Micro-scan completed for library {LibraryId} in {ElapsedMs}ms"
    )]
    private static partial void LogMicroScanCompleted(
        ILogger logger,
        int libraryId,
        long elapsedMs
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Micro-scan failed for library {LibraryId}")]
    private static partial void LogMicroScanFailed(ILogger logger, int libraryId, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Removed {Count} media parts for path: {Path}"
    )]
    private static partial void LogPathRemoved(ILogger logger, string path, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanned path {Path} in library {LibraryId}")]
    private static partial void LogPathScanned(ILogger logger, string path, int libraryId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to scan path: {Path}")]
    private static partial void LogPathScanFailed(ILogger logger, string path, Exception ex);
}
