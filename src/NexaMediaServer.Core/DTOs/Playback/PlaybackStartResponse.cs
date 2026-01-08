// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response describing a created playback session and initial stream plan.
/// </summary>
public sealed class PlaybackStartResponse
{
    /// <summary>
    /// Gets or sets the public identifier of the metadata item being played.
    /// </summary>
    public Guid CurrentMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the public UUID of the metadata item being played.
    /// </summary>
    public Guid? CurrentMetadataItemUuid { get; set; }

    /// <summary>
    /// Gets or sets the title of the metadata item being played.
    /// </summary>
    public string? CurrentItemTitle { get; set; }

    /// <summary>
    /// Gets or sets the original title (e.g., artist) of the metadata item being played.
    /// </summary>
    public string? CurrentItemOriginalTitle { get; set; }

    /// <summary>
    /// Gets or sets the parent title (e.g., album) of the metadata item being played.
    /// </summary>
    public string? CurrentItemParentTitle { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL of the metadata item being played.
    /// </summary>
    public string? CurrentItemThumbUrl { get; set; }

    /// <summary>
    /// Gets or sets the parent's thumbnail URL of the metadata item being played.
    /// Useful for album artwork when tracks have no dedicated art.
    /// </summary>
    public string? CurrentItemParentThumbUrl { get; set; }

    /// <summary>
    /// Gets or sets the metadata type of the item being played.
    /// </summary>
    public MetadataType CurrentItemMetadataType { get; set; }

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

    /// <summary>
    /// Gets or sets the selected and available audio/subtitle tracks.
    /// </summary>
    public TrackSelection? TrackSelection { get; set; }

    /// <summary>
    /// Gets or sets the reasons why transcoding is required (if transcoding).
    /// </summary>
    public TranscodeReason TranscodeReasons { get; set; } = TranscodeReason.None;

    /// <summary>
    /// Gets or sets the adaptive bitrate ladder for this stream, if applicable.
    /// </summary>
    public AbrLadder? AbrLadder { get; set; }

    /// <summary>
    /// Gets or sets the current index within the playlist (0-based).
    /// </summary>
    public int PlaylistIndex { get; set; }

    /// <summary>
    /// Gets or sets the total number of items in the playlist.
    /// </summary>
    public int PlaylistTotalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled.
    /// </summary>
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode is enabled.
    /// </summary>
    public bool Repeat { get; set; }
}
