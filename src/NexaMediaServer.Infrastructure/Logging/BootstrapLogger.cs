// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Serilog;
using Serilog.Events;

namespace NexaMediaServer.Infrastructure.Logging;

/// <summary>
/// Provides a Serilog bootstrap logger for early startup logging.
/// </summary>
public static class BootstrapLogger
{
    /// <summary>
    /// Creates a bootstrap logger used to capture early startup logs.
    /// </summary>
    /// <param name="rollingFilePath">Optional rolling log file path (e.g., /logs/server-.log).</param>
    /// <returns>A configured <see cref="ILogger"/> instance.</returns>
    public static ILogger Create(string? rollingFilePath = null)
    {
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: LoggingTemplates.DefaultOutputTemplate,
                formatProvider: CultureInfo.InvariantCulture
            );

        if (!string.IsNullOrWhiteSpace(rollingFilePath))
        {
            configuration = configuration.WriteTo.File(
                rollingFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: LoggingTemplates.DefaultOutputTemplate,
                formatProvider: CultureInfo.InvariantCulture
            );
        }

        return configuration.CreateLogger();
    }
}
