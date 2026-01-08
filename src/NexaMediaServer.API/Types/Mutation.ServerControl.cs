// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;

using Microsoft.Extensions.Hosting;

using NexaMediaServer.Core.Constants;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL mutation operations for server control.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Initiates a graceful server shutdown. Container orchestrators (Docker, systemd, etc.) will auto-restart the service.
    /// </summary>
    /// <param name="lifetime">The application lifetime service.</param>
    /// <returns>True if shutdown was initiated successfully.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static bool RestartServer([Service] IHostApplicationLifetime lifetime)
    {
        // Trigger graceful shutdown - orchestrator will restart
        lifetime.StopApplication();
        return true;
    }
}
