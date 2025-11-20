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
}
