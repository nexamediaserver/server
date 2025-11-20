// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Serilog;

namespace NexaMediaServer.Infrastructure.Logging;

/// <summary>
/// Provides a Serilog bootstrap logger for early startup logging.
/// </summary>
public static class BootstrapLogger
{
    /// <summary>
    /// Creates a bootstrap logger used to capture early startup logs.
    /// </summary>
    /// <returns>A configured <see cref="ILogger"/> instance.</returns>
    public static ILogger Create()
    {
        // Minimal console bootstrap logger to capture early startup logs
        var config = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture
            );

        return config.CreateLogger();
    }
}
