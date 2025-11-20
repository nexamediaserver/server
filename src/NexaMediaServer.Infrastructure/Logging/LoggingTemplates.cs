// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Logging;

/// <summary>
/// Provides reusable output templates for Serilog sinks so console and file logs stay consistent.
/// </summary>
public static class LoggingTemplates
{
    /// <summary>
    /// Default template containing timestamp, level, environment, machine, process/thread IDs, source context, correlation identifiers, and the message.
    /// </summary>
    public const string DefaultOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] "
        + "({Application}/{EnvironmentName}/{MachineName}/PID:{ProcessId}/TID:{ThreadId}) "
        + "{SourceContext:l} | ReqId={RequestId} CorrelationId={CorrelationId} TraceId={TraceId} | {Message:lj}{NewLine}{Exception}";
}
