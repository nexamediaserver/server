// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;
using System.Text.Json;

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
            MetadataType.AlbumMedium => new AlbumMedium(),
            MetadataType.Track => new Track(),
            MetadataType.Recording => new Recording(),
            MetadataType.AudioWork => new AudioWork(),
            MetadataType.PhotoAlbum => new PhotoAlbum(),
            MetadataType.Photo => new Photo(),
            MetadataType.PictureSet => new PictureSet(),
            MetadataType.Picture => new Picture(),
            MetadataType.BookSeries => new BookSeries(),
            MetadataType.GameFranchise => new GameFranchise(),
            MetadataType.GameSeries => new GameSeries(),
            MetadataType.Collection => new UserCollection(),
            MetadataType.Playlist => new Playlist(),
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
                AlbumMedium => MetadataType.AlbumMedium,
                Track => MetadataType.Track,
                Recording => MetadataType.Recording,
                AudioWork => MetadataType.AudioWork,
                PhotoAlbum => MetadataType.PhotoAlbum,
                Photo => MetadataType.Photo,
                PictureSet => MetadataType.PictureSet,
                Picture => MetadataType.Picture,
                BookSeries => MetadataType.BookSeries,
                GameFranchise => MetadataType.GameFranchise,
                GameSeries => MetadataType.GameSeries,
                UserCollection => MetadataType.Collection,
                Playlist => MetadataType.Playlist,
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

        // Map ExtraFields from entity (Dictionary<string, JsonElement>) to DTO (Dictionary<string, object?>)
        CopyExtraFieldsFromEntity(entity, dto);

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

        // Map ExtraFields from DTO (Dictionary<string, object?>) to entity (Dictionary<string, JsonElement>)
        CopyExtraFields(dto, entity);

        entity.Children = dto.Children.Select(MapToEntity).ToList();
        foreach (var child in entity.Children)
        {
            child.Parent = entity;
        }
    }

    private static void CopyExtraFields(MetadataBaseItem dto, MetadataItem entity)
    {
        if (dto.ExtraFields.Count == 0)
        {
            return;
        }

        foreach (var kvp in dto.ExtraFields)
        {
            if (kvp.Value is null)
            {
                // Skip null values, or could use JsonDocument.Parse("null").RootElement
                continue;
            }

            // Convert object to JsonElement via serialization
            var json = JsonSerializer.Serialize(kvp.Value);
            using var doc = JsonDocument.Parse(json);
            entity.ExtraFields[kvp.Key] = doc.RootElement.Clone();
        }
    }

    private static void CopyExtraFieldsFromEntity(MetadataItem entity, MetadataBaseItem dto)
    {
        if (entity.ExtraFields.Count == 0)
        {
            return;
        }

        foreach (var kvp in entity.ExtraFields)
        {
            // Convert JsonElement to object
            dto.ExtraFields[kvp.Key] = ConvertJsonElement(kvp.Value);
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
            JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText(),
        };
    }
}