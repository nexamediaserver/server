// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents server runtime information for client display.
/// </summary>
[GraphQLName("ServerInfo")]
public sealed class ServerInfo
{
    /// <summary>
    /// Gets the semantic version string of the running server build.
    /// </summary>
    public string VersionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the server is running in Development environment.
    /// </summary>
    public bool IsDevelopment { get; init; }
}
