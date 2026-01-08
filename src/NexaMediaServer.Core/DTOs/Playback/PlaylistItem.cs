// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Represents a single item in a playlist chunk response.
/// </summary>
public sealed class PlaylistItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the playlist item entry.
    /// </summary>
    public int ItemEntryId { get; set; }

    /// <summary>
    /// Gets or sets the metadata item identifier for this playlist entry.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the public UUID of the metadata item.
    /// </summary>
    public Guid MetadataItemUuid { get; set; }

    /// <summary>
    /// Gets or sets the media item ID if pre-selected.
    /// </summary>
    public int? MediaItemId { get; set; }

    /// <summary>
    /// Gets or sets the media part ID if pre-selected.
    /// </summary>
    public int? MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the 0-based index of this item within the playlist.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item has been served to the client.
    /// </summary>
    public bool Served { get; set; }

    /// <summary>
    /// Gets or sets the title of the item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata type (Movie, Episode, Track, etc.).
    /// </summary>
    public string MetadataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration in milliseconds, if known.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URI for the item.
    /// </summary>
    public string? ThumbUri { get; set; }

    /// <summary>
    /// Gets or sets the parent title (e.g., album for tracks, show for episodes).
    /// </summary>
    public string? ParentTitle { get; set; }

    /// <summary>
    /// Gets or sets additional context like episode number or track number.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the resolved playback URL for this playlist entry when available.
    /// </summary>
    public string? PlaybackUrl { get; set; }

    /// <summary>
    /// Gets or sets the primary person (e.g., artist for tracks, director for movies).
    /// </summary>
    public Entities.MetadataItem? PrimaryPerson { get; set; }
}
