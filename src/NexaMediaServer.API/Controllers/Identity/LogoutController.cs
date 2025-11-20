// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaMediaServer.Core.Services.Authentication;
using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;

namespace NexaMediaServer.API.Controllers.Identity;

/// <summary>
/// Handles sign-out by revoking the backing session and clearing auth cookies.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/logout")]
public sealed class LogoutController : ControllerBase
{
    private readonly ISessionService sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutController"/> class.
    /// </summary>
    /// <param name="sessionService">Service used to revoke persistent sessions.</param>
    public LogoutController(ISessionService sessionService)
    {
        this.sessionService = sessionService;
    }

    /// <summary>
    /// Signs out the current session.
    /// </summary>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns>A 204 response once the session has been revoked.</returns>
    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionClaim = this.User.FindFirst(ClaimTypeConstants.SessionId)?.Value;
        if (sessionClaim is not null && Guid.TryParse(sessionClaim, out var sessionId))
        {
            await this
                .sessionService.RevokeSessionAsync(
                    sessionId,
                    "User initiated logout",
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        await this.HttpContext.SignOutAsync().ConfigureAwait(false);
        return this.NoContent();
    }
}
