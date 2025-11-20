// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Identity;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a user in the Nexa Media Server system.
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user was last active.
    /// </summary>
    public DateTime LastActiveAt { get; set; }

    /// <summary>
    /// Updates the last active timestamp to the current UTC time.
    /// </summary>
    public void UpdateLastActive()
    {
        this.LastActiveAt = DateTime.UtcNow;
    }
}
