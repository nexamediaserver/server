// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Polly;
using Polly.Retry;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// HTTP message handler that provides resilience (retry and circuit breaker) using Polly.
/// </summary>
internal sealed class ResilienceDelegatingHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceDelegatingHandler"/> class.
    /// </summary>
    public ResilienceDelegatingHandler()
    {
        this.pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response =>
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout),
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await this.pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }
}
