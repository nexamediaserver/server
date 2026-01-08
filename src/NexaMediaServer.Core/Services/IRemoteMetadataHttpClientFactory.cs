// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Factory for creating pre-configured HttpClient instances for remote metadata agents.
/// Each agent receives its own HttpClient configured with rate limiting, telemetry,
/// resilience policies, and agent-specific settings.
/// </summary>
public interface IRemoteMetadataHttpClientFactory
{
    /// <summary>
    /// Creates a configured HttpClient instance for the specified remote metadata agent.
    /// A fresh client is created on each call for thread safety, while rate limiters
    /// are shared across all clients with the same agent name.
    /// </summary>
    /// <param name="options">HTTP client configuration options including agent name, rate limits, and base address.</param>
    /// <returns>A configured HttpClient instance ready for making metadata API requests.</returns>
    HttpClient CreateClient(RemoteMetadataHttpOptions options);
}
