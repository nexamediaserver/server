// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Serilog.Core;
using Serilog.Events;

namespace NexaMediaServer.Infrastructure.Logging;

/// <summary>
/// Provides a shared logging level switch for dynamic runtime log level changes.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Gets the logging level switch used for runtime log level changes.
    /// </summary>
    public static LoggingLevelSwitch LevelSwitch { get; } = new LoggingLevelSwitch();

    /// <summary>
    /// Sets the minimum log level.
    /// </summary>
    /// <param name="level">The new minimum log level.</param>
    public static void SetMinimumLevel(LogEventLevel level)
    {
        LevelSwitch.MinimumLevel = level;
    }

    /// <summary>
    /// Parses a log level string and sets the minimum level.
    /// </summary>
    /// <param name="levelString">The log level string (Debug, Information, Warning, Error, Fatal).</param>
    /// <returns>True if the level was successfully parsed and set; otherwise, false.</returns>
    public static bool TrySetMinimumLevel(string levelString)
    {
        if (Enum.TryParse<LogEventLevel>(levelString, ignoreCase: true, out var level))
        {
            SetMinimumLevel(level);
            return true;
        }

        return false;
    }
}
