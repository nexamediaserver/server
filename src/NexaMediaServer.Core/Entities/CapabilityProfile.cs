// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations.Schema;
using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a capability declaration provided by a client for a specific session.
/// </summary>
public class CapabilityProfile : AuditableEntity
{
    /// <summary>
    /// Gets or sets the owning session identifier.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the owning session.
    /// </summary>
    public Session Session { get; set; } = null!;

    /// <summary>
    /// Gets or sets a monotonically increasing version for this session's capability declarations.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the device identifier provided by the client, if any.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets a friendly name for the client/device, if provided.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a structured representation of the capability payload from the client.
    /// </summary>
    [Column("CapabilitiesJson")]
    public PlaybackCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC timestamp when the capability profile was declared.
    /// </summary>
    public DateTime DeclaredAt { get; set; } = DateTime.UtcNow;
}
