// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response from a playback resume operation.
/// </summary>
public sealed class PlaybackResumeResponse
{
    /// <summary>
    /// Gets or sets the resumed playback session.
    /// </summary>
    public PlaybackSession Session { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current metadata item's public UUID.
    /// </summary>
    public Guid CurrentMetadataItemUuid { get; set; }

    /// <summary>
    /// Gets or sets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the current capability profile version for the session.
    /// </summary>
    public int CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets the serialized playback plan for the current item.
    /// </summary>
    public string StreamPlanJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL the client should load to resume playback.
    /// </summary>
    public string PlaybackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trickplay track URL when available.
    /// </summary>
    public string? TrickplayUrl { get; set; }

    /// <summary>
    /// Gets or sets the duration of the media item in milliseconds when known.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the last known playhead position in milliseconds.
    /// </summary>
    public long PlayheadMs { get; set; }

    /// <summary>
    /// Gets or sets the last reported playback state.
    /// </summary>
    public string State { get; set; } = "playing";
}
