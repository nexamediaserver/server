// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used to start playback for a metadata item.
/// </summary>
public sealed class PlaybackStartInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier to start playing.
    /// For single item playback, this is the item to play.
    /// For container playback (album, show), this can be the specific child to start with.
    /// </summary>
    [ID("Item")]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets an optional originator identifier for container-based playlists.
    /// When playing an album track or show episode, set this to the parent container ID
    /// to enable playlist navigation through all items in the container.
    /// </summary>
    [ID("Item")]
    public Guid? OriginatorId { get; set; }

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
    /// Gets or sets an optional originator descriptor.
    /// </summary>
    public string? Originator { get; set; }

    /// <summary>
    /// Gets or sets an optional JSON payload describing playback context.
    /// This property is deprecated; use PlaylistType, OriginatorId, Shuffle, and Repeat instead.
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client believes is current.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets an optional capability declaration to upsert.
    /// </summary>
    public PlaybackCapabilityInput? Capability { get; set; }
}

