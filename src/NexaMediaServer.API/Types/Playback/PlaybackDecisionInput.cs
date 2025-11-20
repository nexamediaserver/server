// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used when the client requests a playback decision.
/// </summary>
public sealed class PlaybackDecisionInput
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the current metadata item identifier.
    /// </summary>
    [ID("Item")]
    public Guid CurrentItemId { get; set; }

    /// <summary>
    /// Gets or sets the current playback status.
    /// </summary>
    public string Status { get; set; } = "ended";

    /// <summary>
    /// Gets or sets the current progress in milliseconds.
    /// </summary>
    public long ProgressMs { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client is using.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets an optional capability declaration to upsert.
    /// </summary>
    public PlaybackCapabilityInput? Capability { get; set; }
}
