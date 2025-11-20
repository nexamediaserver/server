// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.API.Services.Authentication;

/// <summary>
/// Logger message helpers for <see cref="SessionCookieAuthenticationEvents"/>.
/// </summary>
internal static partial class SessionCookieAuthenticationEventsLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Missing or invalid session claim on principal"
    )]
    public static partial void MissingSessionClaim(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Session {SessionId} rejected during cookie validation"
    )]
    public static partial void SessionRejected(ILogger logger, Guid sessionId);
}
