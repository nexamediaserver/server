// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Logger message definitions for <see cref="PlaybackService"/>.
/// </summary>
public partial class PlaybackService
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Heartbeat ignored for missing playback session {PlaybackSessionId}"
    )]
    private static partial void LogMissingPlaybackSession(ILogger logger, Guid playbackSessionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFprobe failed for media part {MediaPartId}: {Message}"
    )]
    private static partial void LogProbeFailed(
        ILogger logger,
        int mediaPartId,
        Exception ex,
        string message
    );

    private static void LogProbeFailed(ILogger logger, int mediaPartId, Exception ex)
    {
        LogProbeFailed(logger, mediaPartId, ex, ex.Message);
    }
}
