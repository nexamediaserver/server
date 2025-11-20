// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Provides mapping helpers between <see cref="MetadataItem"/> entities and the strongly typed metadata DTOs.
/// </summary>
public static class MetadataItemMapper
{
    /// <summary>
    /// Maps a <see cref="MetadataItem"/> entity to the corresponding strongly typed DTO.
    /// </summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The strongly typed metadata DTO.</returns>
    public static MetadataBaseItem Map(MetadataItem entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        MetadataBaseItem dto = entity.MetadataType switch
        {
            MetadataType.Movie => new Movie(),
            MetadataType.Show => new Show(),
            MetadataType.Season => new Season(),
            MetadataType.Episode => new Episode(),
            MetadataType.Trailer => new Trailer(),
            MetadataType.Clip => new Clip(),
            MetadataType.BehindTheScenes => new BehindTheScenes(),
            MetadataType.DeletedScene => new DeletedScene(),
            MetadataType.Featurette => new Featurette(),
            MetadataType.Interview => new Interview(),
            MetadataType.Scene => new Scene(),
            MetadataType.ShortForm => new ShortForm(),
            MetadataType.ExtraOther => new ExtraOther(),
            MetadataType.AlbumReleaseGroup => new AlbumReleaseGroup(),
            MetadataType.AlbumRelease => new AlbumRelease(),
            MetadataType.Track => new Track(),
            MetadataType.Recording => new Recording(),
            MetadataType.AudioWork => new AudioWork(),
            MetadataType.Person => new Person(),
            MetadataType.Group => new Group(),
            _ => throw new NotSupportedException(
                $"Metadata type '{entity.MetadataType}' is not supported by the mapper."
            ),
        };

        CopyFromEntity(entity, dto);
        return dto;
    }

    /// <summary>
    /// Maps a strongly typed metadata DTO back to a <see cref="MetadataItem"/> entity.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The constructed entity.</returns>
    public static MetadataItem MapToEntity(MetadataBaseItem dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = new MetadataItem
        {
            MetadataType = dto switch
            {
                Movie => MetadataType.Movie,
                Show => MetadataType.Show,
                Season => MetadataType.Season,
                Episode => MetadataType.Episode,
                Trailer => MetadataType.Trailer,
                Clip => MetadataType.Clip,
                BehindTheScenes => MetadataType.BehindTheScenes,
                DeletedScene => MetadataType.DeletedScene,
                Featurette => MetadataType.Featurette,
                Interview => MetadataType.Interview,
                Scene => MetadataType.Scene,
                ShortForm => MetadataType.ShortForm,
                ExtraOther => MetadataType.ExtraOther,
                AlbumReleaseGroup => MetadataType.AlbumReleaseGroup,
                AlbumRelease => MetadataType.AlbumRelease,
                Track => MetadataType.Track,
                Recording => MetadataType.Recording,
                AudioWork => MetadataType.AudioWork,
                Person => MetadataType.Person,
                Group => MetadataType.Group,
                _ => throw new NotSupportedException(
                    $"DTO type '{dto.GetType().Name}' is not supported by the mapper."
                ),
            },
        };

        CopyToEntity(dto, entity);
        return entity;
    }

    private static void CopyFromEntity(MetadataItem entity, MetadataBaseItem dto)
    {
        dto.Uuid = entity.Uuid;
        dto.Title = entity.Title;
        dto.SortTitle = entity.SortTitle;
        dto.OriginalTitle = entity.OriginalTitle;
        dto.Summary = entity.Summary;
        dto.Tagline = entity.Tagline;
        dto.ContentRating = entity.ContentRating;
        dto.ContentRatingAge = entity.ContentRatingAge;
        dto.ReleaseDate = entity.ReleaseDate;
        dto.Year = entity.Year;
        dto.Index = entity.Index;
        dto.AbsoluteIndex = entity.AbsoluteIndex;
        dto.Duration = entity.Duration;
        dto.ThumbUri = entity.ThumbUri;
        dto.ThumbHash = entity.ThumbHash;
        dto.ArtUri = entity.ArtUri;
        dto.ArtHash = entity.ArtHash;
        dto.LogoUri = entity.LogoUri;
        dto.LogoHash = entity.LogoHash;
        dto.LibrarySectionId = entity.LibrarySectionId;
        dto.LibrarySection = entity.LibrarySection;
        dto.ParentId = entity.ParentId;
        dto.MediaItems = entity.MediaItems;
        dto.Settings = entity.Settings;

        dto.Children = entity.Children?.Select(Map).ToList() ?? [];
        foreach (var child in dto.Children)
        {
            child.Parent = dto;
        }
    }

    private static void CopyToEntity(MetadataBaseItem dto, MetadataItem entity)
    {
        if (dto.Uuid == Guid.Empty)
        {
            dto.Uuid = Guid.NewGuid();
        }

        entity.Uuid = dto.Uuid;
        entity.Title = dto.Title;
        entity.SortTitle = dto.SortTitle;
        entity.OriginalTitle = dto.OriginalTitle;
        entity.Summary = dto.Summary;
        entity.Tagline = dto.Tagline;
        entity.ContentRating = dto.ContentRating;
        entity.ContentRatingAge = dto.ContentRatingAge;
        entity.ReleaseDate = dto.ReleaseDate;
        entity.Year = dto.Year;
        entity.Index = dto.Index;
        entity.AbsoluteIndex = dto.AbsoluteIndex;
        entity.Duration = dto.Duration;
        entity.ThumbUri = dto.ThumbUri;
        entity.ThumbHash = dto.ThumbHash;
        entity.ArtUri = dto.ArtUri;
        entity.ArtHash = dto.ArtHash;
        entity.LogoUri = dto.LogoUri;
        entity.LogoHash = dto.LogoHash;
        entity.LibrarySectionId = dto.LibrarySectionId;
        entity.LibrarySection = dto.LibrarySection ?? entity.LibrarySection ?? null!;
        entity.ParentId = dto.ParentId;
        entity.MediaItems = dto.MediaItems;
        entity.Settings = dto.Settings;

        entity.Children = dto.Children.Select(MapToEntity).ToList();
        foreach (var child in entity.Children)
        {
            child.Parent = entity;
        }
    }
}
