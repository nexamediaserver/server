// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration options for remote metadata agent HTTP clients.
/// </summary>
public sealed record RemoteMetadataHttpOptions
{
    /// <summary>
    /// Gets the unique identifier for the metadata agent (used for logging and telemetry).
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// Gets the base address for API endpoints.
    /// </summary>
    public Uri? BaseAddress { get; init; }

    /// <summary>
    /// Gets the request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the maximum number of requests allowed within the time window.
    /// If null, no rate limiting is applied. Defaults to 10 requests per second.
    /// </summary>
    public int? MaxRequests { get; init; } = 10;

    /// <summary>
    /// Gets the time window for rate limiting. Defaults to 1 second.
    /// </summary>
    public TimeSpan TimeWindow { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets a value indicating whether to allow insecure HTTPS connections
    /// (bypasses SSL certificate validation). Defaults to false.
    /// Use with caution - only enable for trusted local network sources.
    /// </summary>
    public bool AllowInsecureConnections { get; init; }

    /// <summary>
    /// Gets additional HTTP headers to include with requests.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalHeaders { get; init; } =
        new Dictionary<string, string>();
}
