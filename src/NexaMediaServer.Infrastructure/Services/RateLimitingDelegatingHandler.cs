// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// HTTP message handler that enforces rate limiting using a token bucket rate limiter.
/// </summary>
internal sealed partial class RateLimitingDelegatingHandler : DelegatingHandler
{
    private readonly RateLimiter rateLimiter;
    private readonly ILogger<RateLimitingDelegatingHandler> logger;
    private readonly string agentName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingDelegatingHandler"/> class.
    /// </summary>
    /// <param name="rateLimiter">Rate limiter instance to enforce request limits.</param>
    /// <param name="agentName">Name of the agent for logging purposes.</param>
    /// <param name="logger">Logger for rate limiting operations.</param>
    public RateLimitingDelegatingHandler(
        RateLimiter rateLimiter,
        string agentName,
        ILogger<RateLimitingDelegatingHandler> logger)
    {
        this.rateLimiter = rateLimiter;
        this.agentName = agentName;
        this.logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await this.rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);

        if (!lease.IsAcquired)
        {
            LogRateLimitExceeded(this.logger, this.agentName);
            throw new InvalidOperationException($"Rate limit exceeded for metadata agent '{this.agentName}'");
        }

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) && retryAfter > TimeSpan.Zero)
        {
            LogRateLimitDelay(this.logger, this.agentName, retryAfter.TotalMilliseconds);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.rateLimiter.Dispose();
        }

        base.Dispose(disposing);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for metadata agent '{AgentName}'. Request blocked.")]
    private static partial void LogRateLimitExceeded(ILogger logger, string agentName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Rate limit delay for metadata agent '{AgentName}': {DelayMs}ms")]
    private static partial void LogRateLimitDelay(ILogger logger, string agentName, double delayMs);
}
