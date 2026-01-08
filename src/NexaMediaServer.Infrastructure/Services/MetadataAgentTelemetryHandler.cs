// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// HTTP message handler that enriches OpenTelemetry activities with metadata agent-specific tags.
/// </summary>
internal sealed partial class MetadataAgentTelemetryHandler : DelegatingHandler
{
    /// <summary>
    /// Request header key for storing the agent name.
    /// </summary>
    internal const string AgentNameHeader = "X-Metadata-Agent-Name";

    private readonly ILogger<MetadataAgentTelemetryHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataAgentTelemetryHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger for telemetry handler operations.</param>
    public MetadataAgentTelemetryHandler(ILogger<MetadataAgentTelemetryHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        string? agentName = null;

        if (activity != null && request.Headers.TryGetValues(AgentNameHeader, out var values))
        {
            agentName = values.FirstOrDefault();
            if (agentName != null)
            {
                activity.SetTag("metadata.agent.name", agentName);
                activity.SetTag("metadata.agent.request.url", request.RequestUri?.ToString());

                // Remove the header so it doesn't get sent to the actual API
                request.Headers.Remove(AgentNameHeader);
            }
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (activity != null)
        {
            activity.SetTag("http.response.status_code", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode && agentName != null)
            {
                LogAgentRequestFailed(
                    this.logger,
                    agentName,
                    (int)response.StatusCode,
                    request.RequestUri?.ToString() ?? "unknown");
            }
        }

        return response;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Metadata agent '{AgentName}' request failed with status {StatusCode} for URL: {Url}")]
    private static partial void LogAgentRequestFailed(
        ILogger logger,
        string agentName,
        int statusCode,
        string url);
}
