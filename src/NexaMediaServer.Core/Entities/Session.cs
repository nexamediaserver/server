// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a long-lived authenticated session that can be revoked.
/// </summary>
public class Session : AuditableEntity
{
    /// <summary>
    /// Gets or sets the public identifier for this session (embedded in JWTs).
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the user that owns this session.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the owning user navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the device identifier responsible for this session.
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device navigation property.
    /// </summary>
    public Device Device { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC expiration timestamp for the session.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp for when the session was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets optional metadata describing why the session was revoked.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp for the most recent activity observed for this session.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the client application version if reported.
    /// </summary>
    public string? ClientVersion { get; set; }

    /// <summary>
    /// Gets or sets the IP address associated with the session creation.
    /// </summary>
    public string? CreatedFromIp { get; set; }

    /// <summary>
    /// Gets a value indicating whether the session is currently active.
    /// </summary>
    public bool IsActive => this.RevokedAt is null && this.ExpiresAt > DateTime.UtcNow;
}
