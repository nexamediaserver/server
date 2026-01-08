// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Fields;

/// <summary>
/// Provides hardcoded field definitions for different metadata types.
/// This implementation defines the default field layout for item detail pages.
/// </summary>
public sealed class DetailFieldDefinitionProvider : IDetailFieldDefinitionProvider
{
    // Movie field definitions
    private static readonly DetailFieldDefinition[] MovieFields =
    [
        new(DetailFieldType.OriginalTitle, "Original Title", DetailFieldWidgetType.Text, 1),
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 2),
        new(DetailFieldType.Year, "Year", DetailFieldWidgetType.Text, 3, GroupKey: "metadata-row"),
        new(DetailFieldType.Runtime, "Runtime", DetailFieldWidgetType.Duration, 3, GroupKey: "metadata-row"),
        new(DetailFieldType.ContentRating, "Rating", DetailFieldWidgetType.Badge, 3, GroupKey: "metadata-row"),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 4),
        new(DetailFieldType.Genres, "Genres", DetailFieldWidgetType.List, 5),
        new(DetailFieldType.Tagline, "Tagline", DetailFieldWidgetType.Text, 6),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 7),
        new(DetailFieldType.Tags, "Tags", DetailFieldWidgetType.List, 8),
        new(DetailFieldType.ExternalIds, "External IDs", DetailFieldWidgetType.List, 9),
    ];

    // Movie field groups
    private static readonly DetailFieldGroup[] MovieGroups =
    [
        new("metadata-row", "Release Info", DetailFieldGroupLayoutType.Horizontal, 3),
    ];

    // TV Show field definitions
    private static readonly DetailFieldDefinition[] ShowFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.OriginalTitle, "Original Title", DetailFieldWidgetType.Text, 2),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 3),
        new(DetailFieldType.Year, "Year", DetailFieldWidgetType.Text, 4, GroupKey: "metadata-row"),
        new(DetailFieldType.ContentRating, "Rating", DetailFieldWidgetType.Badge, 5, GroupKey: "metadata-row"),
        new(DetailFieldType.Genres, "Genres", DetailFieldWidgetType.List, 6),
        new(DetailFieldType.Tagline, "Tagline", DetailFieldWidgetType.Text, 7),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 8),
        new(DetailFieldType.Tags, "Tags", DetailFieldWidgetType.List, 9),
        new(DetailFieldType.ExternalIds, "External IDs", DetailFieldWidgetType.List, 10),
    ];

    // TV Show field groups
    private static readonly DetailFieldGroup[] ShowGroups =
    [
        new("metadata-row", "Release Info", DetailFieldGroupLayoutType.Horizontal, 1),
    ];

    // Season field definitions
    private static readonly DetailFieldDefinition[] SeasonFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.Year, "Year", DetailFieldWidgetType.Text, 3),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 4),
    ];

    // Episode field definitions
    private static readonly DetailFieldDefinition[] EpisodeFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.ReleaseDate, "Air Date", DetailFieldWidgetType.Date, 3, GroupKey: "metadata-row"),
        new(DetailFieldType.Runtime, "Runtime", DetailFieldWidgetType.Duration, 4, GroupKey: "metadata-row"),
        new(DetailFieldType.ContentRating, "Rating", DetailFieldWidgetType.Badge, 5, GroupKey: "metadata-row"),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 6),
    ];

    // Episode field groups
    private static readonly DetailFieldGroup[] EpisodeGroups =
    [
        new("metadata-row", "Episode Info", DetailFieldGroupLayoutType.Horizontal, 1),
    ];

    // Album (AlbumReleaseGroup) field definitions
    private static readonly DetailFieldDefinition[] AlbumFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.Year, "Year", DetailFieldWidgetType.Text, 3),
        new(DetailFieldType.Genres, "Genres", DetailFieldWidgetType.List, 4),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 5),
        new(DetailFieldType.Tags, "Tags", DetailFieldWidgetType.List, 6),
        new(DetailFieldType.ExternalIds, "External IDs", DetailFieldWidgetType.List, 7),
    ];

    // Track field definitions
    private static readonly DetailFieldDefinition[] TrackFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.Runtime, "Duration", DetailFieldWidgetType.Duration, 3),
    ];

    // Person field definitions
    private static readonly DetailFieldDefinition[] PersonFields =
    [
        new(DetailFieldType.Title, "Name", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Summary, "Biography", DetailFieldWidgetType.Text, 2),
        new(DetailFieldType.ExternalIds, "External IDs", DetailFieldWidgetType.List, 3),
    ];

    // Photo/Picture field definitions
    private static readonly DetailFieldDefinition[] PhotoFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.ReleaseDate, "Date Taken", DetailFieldWidgetType.Date, 3),
        new(DetailFieldType.Summary, "Description", DetailFieldWidgetType.Text, 4),
        new(DetailFieldType.Tags, "Tags", DetailFieldWidgetType.List, 5),
    ];

    // Default/fallback field definitions
    private static readonly DetailFieldDefinition[] DefaultFields =
    [
        new(DetailFieldType.Title, "Title", DetailFieldWidgetType.Heading, 1),
        new(DetailFieldType.Actions, "Actions", DetailFieldWidgetType.Actions, 2),
        new(DetailFieldType.Summary, "Summary", DetailFieldWidgetType.Text, 3),
    ];

    // Default/fallback field groups (empty)
    private static readonly DetailFieldGroup[] DefaultGroups = [];

    /// <inheritdoc/>
    public IReadOnlyList<DetailFieldDefinition> GetDefaultFields(MetadataType metadataType)
    {
        return metadataType switch
        {
            MetadataType.Movie => MovieFields,
            MetadataType.Show => ShowFields,
            MetadataType.Season => SeasonFields,
            MetadataType.Episode => EpisodeFields,
            MetadataType.AlbumReleaseGroup => AlbumFields,
            MetadataType.AlbumRelease => AlbumFields,
            MetadataType.Track or MetadataType.Recording => TrackFields,
            MetadataType.Person => PersonFields,
            MetadataType.Photo or MetadataType.Picture => PhotoFields,
            MetadataType.PhotoAlbum or MetadataType.PictureSet => PhotoFields,
            _ => DefaultFields,
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<DetailFieldGroup> GetDefaultGroups(MetadataType metadataType)
    {
        return metadataType switch
        {
            MetadataType.Movie => MovieGroups,
            MetadataType.Show => ShowGroups,
            MetadataType.Season => DefaultGroups,
            MetadataType.Episode => EpisodeGroups,
            MetadataType.AlbumReleaseGroup => DefaultGroups,
            MetadataType.AlbumRelease => DefaultGroups,
            MetadataType.Track or MetadataType.Recording => DefaultGroups,
            MetadataType.Person => DefaultGroups,
            MetadataType.Photo or MetadataType.Picture => DefaultGroups,
            MetadataType.PhotoAlbum or MetadataType.PictureSet => DefaultGroups,
            _ => DefaultGroups,
        };
    }
}
