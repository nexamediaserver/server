// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload returned after starting playback.
/// </summary>
public sealed class PlaybackStartPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartPayload"/> class.
    /// </summary>
    /// <param name="response">Playback start response from the service.</param>
    /// <param name="capabilityVersionMismatch">Indicates whether the client's capability version is stale.</param>
    public PlaybackStartPayload(PlaybackStartResponse response, bool capabilityVersionMismatch)
    {
        this.PlaybackSessionId = response.PlaybackSessionId;
        this.PlaylistGeneratorId = response.PlaylistGeneratorId;
        this.CapabilityProfileVersion = response.CapabilityProfileVersion;
        this.StreamPlanJson = response.StreamPlanJson;
        this.PlaybackUrl = response.PlaybackUrl;
        this.TrickplayUrl = response.TrickplayUrl;
        this.DurationMs = response.DurationMs;
        this.CapabilityVersionMismatch = capabilityVersionMismatch;
    }

    /// <summary>
    /// Gets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; }

    /// <summary>
    /// Gets the playlist generator identifier.
    /// </summary>
    public Guid PlaylistGeneratorId { get; }

    /// <summary>
    /// Gets the capability profile version the server used.
    /// </summary>
    public int CapabilityProfileVersion { get; }

    /// <summary>
    /// Gets the serialized stream plan for the current item.
    /// </summary>
    public string StreamPlanJson { get; }

    /// <summary>
    /// Gets the URL the client should load to start playback.
    /// </summary>
    public string PlaybackUrl { get; }

    /// <summary>
    /// Gets the trickplay thumbnail track URL when available.
    /// </summary>
    public string? TrickplayUrl { get; }

    /// <summary>
    /// Gets the duration of the media item in milliseconds.
    /// </summary>
    /// <remarks>
    /// This value is authoritative and should be preferred over browser-reported
    /// duration, which may be incorrect for remuxed or transcoded streams.
    /// </remarks>
    public long? DurationMs { get; }

    /// <summary>
    /// Gets a value indicating whether the client should refresh capabilities.
    /// </summary>
    public bool CapabilityVersionMismatch { get; }
}
