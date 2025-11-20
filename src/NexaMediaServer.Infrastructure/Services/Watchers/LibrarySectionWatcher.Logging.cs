// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Logger message definitions for <see cref="LibrarySectionWatcher"/>.
/// </summary>
internal sealed partial class LibrarySectionWatcher
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Location path not found: {Path}")]
    private static partial void LogLocationNotFound(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Watcher error for library {LibraryId}, location {LocationId}"
    )]
    private static partial void LogWatcherError(
        ILogger logger,
        int libraryId,
        int locationId,
        Exception ex
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Too many watcher errors for library {LibraryId}, triggering full rescan"
    )]
    private static partial void LogTooManyErrors(ILogger logger, int libraryId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error during deep directory polling for library {LibraryId}"
    )]
    private static partial void LogPollingError(ILogger logger, int libraryId, Exception ex);
}
