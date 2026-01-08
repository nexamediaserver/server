// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Request to start playback for a given metadata item.
/// </summary>
public sealed class PlaybackStartRequest
{
    /// <summary>
    /// Gets or sets the database identifier of the metadata item to play.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the database identifier of the originator container for playlist-based playback.
    /// For album tracks, this is the album ID. For episodes, this could be the season or show ID.
    /// </summary>
    public int? OriginatorMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the playlist type. Defaults to "single" for single item playback.
    /// Supported values: "single", "album", "season", "show", "artist", "library", "explicit".
    /// </summary>
    public string PlaylistType { get; set; } = "single";

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode should be enabled for the playlist.
    /// </summary>
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode should be enabled for the playlist.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Gets or sets an optional originator descriptor (user action, remote control, etc.).
    /// </summary>
    public string? Originator { get; set; }

    /// <summary>
    /// Gets or sets an optional JSON blob describing playback context (shuffle, repeat, resume).
    /// This property is deprecated; use PlaylistType, OriginatorMetadataItemId, Shuffle, and Repeat instead.
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client believes is current.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }
}

