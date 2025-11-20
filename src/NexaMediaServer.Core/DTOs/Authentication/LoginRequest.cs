// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Represents the payload required to authenticate a user via email/password and register device metadata.
/// </summary>
public sealed record class LoginRequest
{
    /// <summary>
    /// Gets the email (or username) used for authentication.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password used for authentication.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets the client device metadata required to create a session.
    /// </summary>
    [Required]
    public DeviceRegistration? Device { get; init; }

    /// <summary>
    /// Gets a value indicating whether the authentication cookie should persist beyond the session.
    /// </summary>
    public bool RememberMe { get; init; }
}
