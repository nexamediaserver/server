// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services.Authentication;
using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;

namespace NexaMediaServer.API.Services.Authentication;

/// <summary>
/// Cookie authentication events that ensure backing sessions remain valid.
/// </summary>
public sealed class SessionCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ISessionService sessionService;
    private readonly ILogger<SessionCookieAuthenticationEvents> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCookieAuthenticationEvents"/> class.
    /// </summary>
    /// <param name="sessionService">Service responsible for validating persisted sessions.</param>
    /// <param name="logger">The logger used for diagnostic events.</param>
    public SessionCookieAuthenticationEvents(
        ISessionService sessionService,
        ILogger<SessionCookieAuthenticationEvents> logger
    )
    {
        this.sessionService = sessionService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sessionClaimValue = context.Principal?.FindFirstValue(ClaimTypeConstants.SessionId);
        if (
            string.IsNullOrWhiteSpace(sessionClaimValue)
            || !Guid.TryParse(sessionClaimValue, out var sessionId)
        )
        {
            SessionCookieAuthenticationEventsLog.MissingSessionClaim(this.logger);
            await SignOutAsync(context).ConfigureAwait(false);
            return;
        }

        var session = await this
            .sessionService.ValidateSessionAsync(sessionId, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (session is null)
        {
            SessionCookieAuthenticationEventsLog.SessionRejected(this.logger, sessionId);
            await SignOutAsync(context).ConfigureAwait(false);
        }
    }

    private static async Task SignOutAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name).ConfigureAwait(false);
    }
}
