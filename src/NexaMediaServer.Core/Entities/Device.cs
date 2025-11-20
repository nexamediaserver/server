// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a device that connects to the media server.
/// </summary>
public class Device : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier assigned to the device by the client.
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the friendly name that identifies the device.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the platform that the device is running on.
    /// </summary>
    public string Platform { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional version string reported by the device.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the IP address that last registered this device (if available).
    /// </summary>
    public string? LastSeenIp { get; set; }

    /// <summary>
    /// Gets or sets the user identifier that owns this device.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the owning user navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of sessions established on this device.
    /// </summary>
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
