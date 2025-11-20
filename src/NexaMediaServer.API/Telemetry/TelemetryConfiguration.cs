// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using NexaMediaServer.Infrastructure.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NexaMediaServer.API.Telemetry;

/// <summary>
/// Configuration for OpenTelemetry tracing and metrics.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// Adds OpenTelemetry telemetry services to the application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: "Nexa Media Server",
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                    ?? "unknown",
                    autoGenerateServiceInstanceId: false
                );
            })
            .WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHotChocolateInstrumentation()
                    .AddHangfireInstrumentation()
                    .AddSource(ActivitySourceProvider.SourceName);
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddMeter(ActivitySourceProvider.SourceName)
                    .AddPrometheusExporter();
            });

        return builder;
    }
}
