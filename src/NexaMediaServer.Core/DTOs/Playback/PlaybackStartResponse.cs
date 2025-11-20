// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response describing a created playback session and initial stream plan.
/// </summary>
public sealed class PlaybackStartResponse
{
    /// <summary>
    /// Gets or sets the public playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the playlist generator identifier associated with this session.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version used to plan playback.
    /// </summary>
    public int CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets the serialized stream plan payload for the first item.
    /// </summary>
    public string StreamPlanJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the URL the client should load to begin playback.
    /// </summary>
    public string PlaybackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trickplay thumbnail track URL, if available.
    /// </summary>
    public string? TrickplayUrl { get; set; }

    /// <summary>
    /// Gets or sets the duration of the media item in milliseconds.
    /// </summary>
    /// <remarks>
    /// This value comes from the server's metadata and should be used
    /// as the authoritative duration, since streamed/transcoded media
    /// may not report duration correctly to the browser.
    /// </remarks>
    public long? DurationMs { get; set; }
}
