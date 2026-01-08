// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload representing a single playlist item.
/// </summary>
public sealed class PlaylistItemPayload
{
    /// <summary>
    /// Gets or sets the unique identifier of the playlist item entry.
    /// </summary>
    public int ItemEntryId { get; set; }

    /// <summary>
    /// Gets or sets the public UUID of the metadata item.
    /// </summary>
    [ID("Item")]
    public Guid ItemId { get; set; }

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
    /// Gets or sets the playback URL for this playlist entry when precomputed (e.g., images).
    /// </summary>
    public string? PlaybackUrl { get; set; }

    /// <summary>
    /// Gets or sets the primary person (e.g., artist for tracks, director for movies).
    /// </summary>
    public MetadataItem? PrimaryPerson { get; set; }

    /// <summary>
    /// Creates a payload from a DTO.
    /// </summary>
    /// <param name="item">The playlist item DTO.</param>
    /// <returns>The GraphQL payload.</returns>
    public static PlaylistItemPayload FromDto(PlaylistItem item)
    {
        MetadataItem? primaryPerson = null;
        if (item.PrimaryPerson != null)
        {
            primaryPerson = new MetadataItem
            {
                Id = item.PrimaryPerson.Uuid,
                DatabaseId = item.PrimaryPerson.Id,
                MetadataType = item.PrimaryPerson.MetadataType,
                Title = item.PrimaryPerson.Title,
                TitleSort = item.PrimaryPerson.SortTitle,
            };
        }

        return new PlaylistItemPayload
        {
            ItemEntryId = item.ItemEntryId,
            ItemId = item.MetadataItemUuid,
            Index = item.Index,
            Served = item.Served,
            Title = item.Title,
            MetadataType = item.MetadataType,
            DurationMs = item.DurationMs,
            ThumbUri = item.ThumbUri,
            ParentTitle = item.ParentTitle,
            Subtitle = item.Subtitle,
            PlaybackUrl = item.PlaybackUrl,
            PrimaryPerson = primaryPerson,
        };
    }
}
