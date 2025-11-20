// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Logger message definitions for <see cref="WatcherEventBuffer"/>.
/// </summary>
public sealed partial class WatcherEventBuffer
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Event dropped due to buffer overflow: {Path} in library {LibraryId}"
    )]
    private static partial void LogEventDropped(ILogger logger, string path, int libraryId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing filesystem events")]
    private static partial void LogProcessingError(ILogger logger, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Dispatched micro-scan job for library {LibraryId}: "
            + "{ScanCount} paths to scan, {RemoveCount} paths to remove"
    )]
    private static partial void LogMicroScanDispatched(
        ILogger logger,
        int libraryId,
        int scanCount,
        int removeCount
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Dispatched full rescan job for library {LibraryId} due to watcher errors"
    )]
    private static partial void LogFullRescanDispatched(ILogger logger, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to dispatch job for library {LibraryId}"
    )]
    private static partial void LogDispatchError(ILogger logger, int libraryId, Exception ex);
}
