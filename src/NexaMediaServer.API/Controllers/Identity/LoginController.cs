// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NexaMediaServer.Core.DTOs.Authentication;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services.Authentication;
using ClaimTypeConstants = NexaMediaServer.Core.Constants.ClaimTypes;

namespace NexaMediaServer.API.Controllers.Identity;

/// <summary>
/// Provides cookie-based login functionality that mirrors the legacy Identity API endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/login")]
public sealed class LoginController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;
    private readonly ISessionService sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginController"/> class.
    /// </summary>
    /// <param name="userManager">Manages user persistence and lookups.</param>
    /// <param name="signInManager">Handles issuing authentication cookies.</param>
    /// <param name="sessionService">Creates and validates persistent sessions.</param>
    public LoginController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ISessionService sessionService
    )
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.sessionService = sessionService;
    }

    /// <summary>
    /// Authenticates a user and issues a cookie-backed session.
    /// </summary>
    /// <param name="request">Login payload containing credentials and device metadata.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns>A 204 response when login succeeds; otherwise an error result.</returns>
    [HttpPost]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            this.ModelState.AddModelError(string.Empty, "Request body is required.");
            return this.ValidationProblem(this.ModelState);
        }

        if (!this.ModelState.IsValid)
        {
            return this.ValidationProblem(this.ModelState);
        }

        if (request.Device is null)
        {
            this.ModelState.AddModelError(nameof(request.Device), "Device metadata is required.");
            return this.ValidationProblem(this.ModelState);
        }

        var user = await this.userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            return this.Unauthorized();
        }

        var passwordResult = await this
            .signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (!passwordResult.Succeeded)
        {
            if (passwordResult.IsLockedOut || passwordResult.IsNotAllowed)
            {
                return this.Forbid();
            }

            return this.Unauthorized();
        }

        var deviceMetadata = request.Device!;
        var device = await this
            .sessionService.UpsertDeviceAsync(user.Id, deviceMetadata, cancellationToken)
            .ConfigureAwait(false);

        var session = await this
            .sessionService.CreateSessionAsync(
                new SessionCreationRequest(user.Id, device.Id, deviceMetadata.Version),
                cancellationToken
            )
            .ConfigureAwait(false);

        user.UpdateLastActive();
        var updateResult = await this.userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            this.AddIdentityErrors(updateResult);
            return this.ValidationProblem(this.ModelState);
        }

        var additionalClaims = new List<Claim>
        {
            new(ClaimTypeConstants.SessionId, session.PublicId.ToString()),
        };

        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = request.RememberMe,
            ExpiresUtc = new DateTimeOffset(
                DateTime.SpecifyKind(session.ExpiresAt, DateTimeKind.Utc)
            ),
        };

        await this
            .signInManager.SignInWithClaimsAsync(user, properties, additionalClaims)
            .ConfigureAwait(false);

        return this.NoContent();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            this.ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
