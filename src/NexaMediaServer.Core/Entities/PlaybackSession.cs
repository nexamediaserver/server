// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Constants;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents an active playback session tied to a user session and capability profile.
/// </summary>
public class PlaybackSession : AuditableEntity
{
    /// <summary>
    /// Gets or sets the public identifier for correlation across heartbeats and decisions.
    /// </summary>
    public Guid PlaybackSessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the owning authenticated session identifier.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the owning authenticated session.
    /// </summary>
    public Session Session { get; set; } = null!;

    /// <summary>
    /// Gets or sets the capability profile identifier used to plan this playback.
    /// </summary>
    public int CapabilityProfileId { get; set; }

    /// <summary>
    /// Gets or sets the capability profile used for this playback.
    /// </summary>
    public CapabilityProfile CapabilityProfile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the metadata item being played currently.
    /// </summary>
    public int CurrentMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the associated metadata item.
    /// </summary>
    public MetadataItem CurrentMetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the media part being streamed, if known.
    /// </summary>
    public int? CurrentMediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the media part reference.
    /// </summary>
    public MediaPart? CurrentMediaPart { get; set; }

    /// <summary>
    /// Gets or sets an optional originator string describing who/what initiated playback.
    /// </summary>
    public string? Originator { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the last heartbeat or decision.
    /// </summary>
    public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when this session expires due to inactivity.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);

    /// <summary>
    /// Gets or sets the current playhead position in milliseconds.
    /// </summary>
    public long PlayheadMs { get; set; }

    /// <summary>
    /// Gets or sets the current playback state reported by the client (playing, paused, buffering, stopped).
    /// </summary>
    public string State { get; set; } = "playing";

    /// <summary>
    /// Gets or sets the playlist generator associated with this playback session.
    /// </summary>
    public PlaylistGenerator? PlaylistGenerator { get; set; }
}
