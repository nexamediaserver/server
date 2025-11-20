// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
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
    /// Gets or sets the collection of devices registered by the user.
    /// </summary>
    public ICollection<Device> Devices { get; set; } = new List<Device>();

    /// <summary>
    /// Gets or sets the collection of sessions issued for the user.
    /// </summary>
    public ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>
    /// Updates the last active timestamp to the current UTC time.
    /// </summary>
    public void UpdateLastActive()
    {
        this.LastActiveAt = DateTime.UtcNow;
    }
}
