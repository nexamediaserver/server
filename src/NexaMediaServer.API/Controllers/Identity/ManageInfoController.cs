// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using NexaMediaServer.Core.DTOs.Authentication;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.API.Controllers.Identity;

/// <summary>
/// Provides endpoints for reading and updating profile information.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/manage/info")]
public sealed class ManageInfoController : ControllerBase
{
    private readonly UserManager<User> userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageInfoController"/> class.
    /// </summary>
    /// <param name="userManager">Manages user persistence and profile updates.</param>
    public ManageInfoController(UserManager<User> userManager)
    {
        this.userManager = userManager;
    }

    /// <summary>
    /// Gets the current user's profile details.
    /// </summary>
    /// <returns>The authenticated user's profile summary.</returns>
    [HttpGet]
    public async Task<ActionResult<InfoResponse>> GetAsync()
    {
        var user = await this.GetCurrentUserAsync().ConfigureAwait(false);
        if (user is null)
        {
            return this.Unauthorized();
        }

        var roles = await this.userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new InfoResponse(user.Email ?? string.Empty, user.EmailConfirmed, roles.ToArray());
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    /// <param name="request">Profile change request.</param>
    /// <returns>The updated profile snapshot.</returns>
    [HttpPost]
    public async Task<ActionResult<InfoResponse>> UpdateAsync([FromBody] InfoRequest? request)
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

        var user = await this.GetCurrentUserAsync().ConfigureAwait(false);
        if (user is null)
        {
            return this.Unauthorized();
        }

        var hasChanges = false;
        if (
            !string.IsNullOrWhiteSpace(request.NewEmail)
            && !string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase)
        )
        {
            user.Email = request.NewEmail;
            user.UserName = request.NewEmail;
            user.EmailConfirmed = false;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                this.ModelState.AddModelError(
                    nameof(request.OldPassword),
                    "Old password is required when setting a new password."
                );
                return this.ValidationProblem(this.ModelState);
            }

            var passwordResult = await this
                .userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword)
                .ConfigureAwait(false);
            if (!passwordResult.Succeeded)
            {
                this.AddIdentityErrors(passwordResult);
                return this.ValidationProblem(this.ModelState);
            }
        }

        if (hasChanges)
        {
            var updateResult = await this.userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                this.AddIdentityErrors(updateResult);
                return this.ValidationProblem(this.ModelState);
            }
        }

        var updatedRoles = await this.userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new InfoResponse(user.Email ?? string.Empty, user.EmailConfirmed, updatedRoles.ToArray());
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        return await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            this.ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
