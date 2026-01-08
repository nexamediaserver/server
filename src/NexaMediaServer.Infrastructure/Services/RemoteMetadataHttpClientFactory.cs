// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Factory implementation for creating configured HttpClient instances for remote metadata agents.
/// </summary>
internal sealed class RemoteMetadataHttpClientFactory : IRemoteMetadataHttpClientFactory
{
    private static readonly string UserAgentBase = BuildUserAgentBase();
#pragma warning disable S4487 // Unused private types or members should be removed - logger is injected for future use
    private readonly ILogger<RemoteMetadataHttpClientFactory> logger;
#pragma warning restore S4487
    private readonly ILogger<RateLimitingDelegatingHandler> rateLimitLogger;
    private readonly ConcurrentDictionary<string, RateLimiter> rateLimiters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteMetadataHttpClientFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for factory operations.</param>
    /// <param name="rateLimitLogger">Logger for rate limiting handler.</param>
#pragma warning disable S6672 // Logger types should match their enclosing types - rateLimitLogger intentionally uses RateLimitingDelegatingHandler type
    public RemoteMetadataHttpClientFactory(
        ILogger<RemoteMetadataHttpClientFactory> logger,
        ILogger<RateLimitingDelegatingHandler> rateLimitLogger)
#pragma warning restore S6672
    {
        this.logger = logger;
        this.rateLimitLogger = rateLimitLogger;
    }

    /// <inheritdoc/>
    public HttpClient CreateClient(RemoteMetadataHttpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Build handler chain starting from the innermost handler
        HttpMessageHandler handler;

        // Start with optional SSL bypass
        if (options.AllowInsecureConnections)
        {
#pragma warning disable S4830 // Server certificate validation is intentionally disabled when AllowInsecureConnections is true
            handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
#pragma warning restore S4830
        }
        else
        {
            handler = new HttpClientHandler();
        }

        // Add resilience (retry and timeout)
        handler = new ResilienceDelegatingHandler
        {
            InnerHandler = handler,
        };

        // Add telemetry (using NullLogger since we primarily rely on OpenTelemetry instrumentation)
        handler = new MetadataAgentTelemetryHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MetadataAgentTelemetryHandler>.Instance)
        {
            InnerHandler = handler,
        };

        // Add rate limiting if configured
        if (options.MaxRequests.HasValue && options.MaxRequests > 0)
        {
            var rateLimiter = this.GetOrCreateRateLimiter(options.AgentName, options.MaxRequests.Value, options.TimeWindow);
            handler = new RateLimitingDelegatingHandler(rateLimiter, options.AgentName, this.rateLimitLogger)
            {
                InnerHandler = handler,
            };
        }

        // Create new HttpClient with configured handler chain
        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = options.Timeout,
        };

        // Configure base address
        if (options.BaseAddress != null)
        {
            client.BaseAddress = options.BaseAddress;
        }

        // Set user agent with agent name
        var userAgent = $"{UserAgentBase} ({options.AgentName})";
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        // Add additional headers
        foreach (var header in options.AdditionalHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add agent name header for telemetry (will be removed by handler before sending)
        client.DefaultRequestHeaders.Add(MetadataAgentTelemetryHandler.AgentNameHeader, options.AgentName);

        return client;
    }

    private static string BuildUserAgentBase()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";

        var frameworkDescription = RuntimeInformation.FrameworkDescription;
        var osDescription = RuntimeInformation.OSDescription;

        return $"NexaMediaServer/{version} ({frameworkDescription}; {osDescription})";
    }

    private RateLimiter GetOrCreateRateLimiter(string agentName, int maxRequests, TimeSpan timeWindow)
    {
        return this.rateLimiters.GetOrAdd(agentName, _ =>
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = maxRequests,
                ReplenishmentPeriod = timeWindow,
                TokensPerPeriod = maxRequests,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10, // Allow some queueing to smooth bursts
            };

            return new TokenBucketRateLimiter(options);
        });
    }
}
