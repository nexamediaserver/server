// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Telemetry;

/// <summary>
/// Provides a centralized ActivitySource for telemetry and distributed tracing.
/// </summary>
public static class ActivitySourceProvider
{
    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public const string SourceName = "Nexa Media Server";

    /// <summary>
    /// The version of the activity source.
    /// </summary>
    public const string SourceVersion = "1.0.0";

    private static readonly Lazy<System.Diagnostics.ActivitySource> Source = new(() =>
        new System.Diagnostics.ActivitySource(SourceName, SourceVersion)
    );

    /// <summary>
    /// Gets the shared ActivitySource instance.
    /// </summary>
    public static System.Diagnostics.ActivitySource Instance => Source.Value;
}
