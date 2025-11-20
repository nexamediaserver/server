// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Represents a request to update identity profile information.
/// </summary>
public sealed record class InfoRequest
{
    /// <summary>
    /// Gets the optional new email address.
    /// </summary>
    [EmailAddress]
    public string? NewEmail { get; init; }

    /// <summary>
    /// Gets the optional new password.
    /// </summary>
    [MinLength(6)]
    public string? NewPassword { get; init; }

    /// <summary>
    /// Gets the current password when changing passwords.
    /// </summary>
    public string? OldPassword { get; init; }
}
