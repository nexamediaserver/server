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
        this.CurrentItemId = response.CurrentMetadataItemUuid;
        this.CurrentItemMetadataType = response.CurrentItemMetadataType.ToString();
        this.CurrentItemOriginalTitle = response.CurrentItemOriginalTitle;
        this.CurrentItemParentThumbUrl = response.CurrentItemParentThumbUrl;
        this.CurrentItemParentTitle = response.CurrentItemParentTitle;
        this.CurrentItemThumbUrl = response.CurrentItemThumbUrl;
        this.CurrentItemTitle = response.CurrentItemTitle;
        this.PlaybackSessionId = response.PlaybackSessionId;
        this.PlaylistGeneratorId = response.PlaylistGeneratorId;
        this.CapabilityProfileVersion = response.CapabilityProfileVersion;
        this.StreamPlanJson = response.StreamPlanJson;
        this.PlaybackUrl = response.PlaybackUrl;
        this.TrickplayUrl = response.TrickplayUrl;
        this.DurationMs = response.DurationMs;
        this.CapabilityVersionMismatch = capabilityVersionMismatch;
        this.PlaylistIndex = response.PlaylistIndex;
        this.PlaylistTotalCount = response.PlaylistTotalCount;
        this.Shuffle = response.Shuffle;
        this.Repeat = response.Repeat;
    }

    /// <summary>
    /// Gets the current item's identifier (public UUID).
    /// </summary>
    [ID("Item")]
    public Guid? CurrentItemId { get; }

    /// <summary>
    /// Gets the metadata type of the current item.
    /// </summary>
    public string CurrentItemMetadataType { get; }

    /// <summary>
    /// Gets the title of the current item being played.
    /// </summary>
    public string? CurrentItemTitle { get; }

    /// <summary>
    /// Gets the original title (e.g., artist) of the current item being played.
    /// </summary>
    public string? CurrentItemOriginalTitle { get; }

    /// <summary>
    /// Gets the parent title (e.g., album) of the current item being played.
    /// </summary>
    public string? CurrentItemParentTitle { get; }

    /// <summary>
    /// Gets the thumbnail URL of the current item being played.
    /// </summary>
    public string? CurrentItemThumbUrl { get; }

    /// <summary>
    /// Gets the parent thumbnail URL of the current item being played.
    /// </summary>
    public string? CurrentItemParentThumbUrl { get; }

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

    /// <summary>
    /// Gets the current index within the playlist (0-based).
    /// </summary>
    public int PlaylistIndex { get; }

    /// <summary>
    /// Gets the total number of items in the playlist.
    /// </summary>
    public int PlaylistTotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether shuffle mode is enabled.
    /// </summary>
    public bool Shuffle { get; }

    /// <summary>
    /// Gets a value indicating whether repeat mode is enabled.
    /// </summary>
    public bool Repeat { get; }
}
