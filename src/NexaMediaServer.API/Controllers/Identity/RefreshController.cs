// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NexaMediaServer.Core.DTOs.Authentication;
using NexaMediaServer.Core.Services.Authentication;
using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;

namespace NexaMediaServer.API.Controllers.Identity;

/// <summary>
/// Confirms an authenticated cookie session remains valid.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/refresh")]
public sealed class RefreshController : ControllerBase
{
    private readonly ISessionService sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshController"/> class.
    /// </summary>
    /// <param name="sessionService">Service used to validate active sessions.</param>
    public RefreshController(ISessionService sessionService)
    {
        this.sessionService = sessionService;
    }

    /// <summary>
    /// Validates the backing session for the authenticated cookie.
    /// </summary>
    /// <param name="request">Optional refresh payload for backwards compatibility.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns>A 204 response when the cookie session is still valid.</returns>
    [HttpPost]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        _ = request;
        var claimValue = this.User.FindFirst(ClaimTypeConstants.SessionId)?.Value;
        if (string.IsNullOrWhiteSpace(claimValue) || !Guid.TryParse(claimValue, out var sessionId))
        {
            await this
                .HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme)
                .ConfigureAwait(false);
            return this.Unauthorized();
        }

        var session = await this
            .sessionService.ValidateSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            await this
                .HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme)
                .ConfigureAwait(false);
            return this.Unauthorized();
        }

        return this.NoContent();
    }
}
