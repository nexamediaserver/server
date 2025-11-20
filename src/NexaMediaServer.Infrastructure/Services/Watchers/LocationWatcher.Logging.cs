// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Logger message definitions for <see cref="LocationWatcher"/>.
/// </summary>
internal sealed partial class LocationWatcher
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Started watching {Path} with {WatcherCount} watchers"
    )]
    private static partial void LogWatcherStarted(ILogger logger, string path, int watcherCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to create watcher for {Path}")]
    private static partial void LogWatcherCreateError(ILogger logger, string path, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error polling directory {Path}")]
    private static partial void LogPollingError(ILogger logger, string path, Exception ex);
}
