// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used when sending playback heartbeats.
/// </summary>
public sealed class PlaybackHeartbeatInput
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the playhead position in milliseconds.
    /// </summary>
    public long PlayheadMs { get; set; }

    /// <summary>
    /// Gets or sets the playback state.
    /// </summary>
    public string State { get; set; } = "playing";

    /// <summary>
    /// Gets or sets the current media part identifier, if known.
    /// </summary>
    public int? MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client is using.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets an optional capability declaration to upsert.
    /// </summary>
    public PlaybackCapabilityInput? Capability { get; set; }
}
