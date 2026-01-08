// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

using NexaMediaServer.Common;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// Helpers for selecting playback formats and media types based on extensions and metadata.
/// </summary>
public static class PlaybackFormatSelector
{
    /// <summary>
    /// Normalizes an extension by trimming, removing leading dots, and canonicalizing common aliases.
    /// </summary>
    /// <param name="ext">The extension to normalize.</param>
    /// <returns>The normalized extension in lowercase without leading dot.</returns>
    public static string NormalizeExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return string.Empty;
        }

        var trimmed = ext.Trim().TrimStart('.').ToLowerInvariant();
        return trimmed switch
        {
            "jpeg" => "jpg",
            _ => trimmed,
        };
    }

    /// <summary>
    /// Resolves the media type (Video, Audio, Photo) for a metadata item based on metadata and file extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <param name="metadataType">The metadata type.</param>
    /// <returns>The resolved media type string.</returns>
    public static string ResolveMediaType(string? extension, MetadataType metadataType)
    {
        var normalizedExt = NormalizeExtension(extension);
        if (IsAudio(metadataType) || MediaFileExtensions.IsAudio($".{normalizedExt}"))
        {
            return "Audio";
        }

        if (IsPhoto(metadataType) || MediaFileExtensions.IsImage($".{normalizedExt}"))
        {
            return "Photo";
        }

        return "Video";
    }

    /// <summary>
    /// Chooses the best image format to serve: original if supported, otherwise WebP preferred over JPEG.
    /// </summary>
    /// <param name="sourceExtension">The source file extension.</param>
    /// <param name="supportedFormats">Formats the client reports supporting.</param>
    /// <returns>The selected target format.</returns>
    public static string ChooseImageFormat(
        string sourceExtension,
        IEnumerable<string> supportedFormats
    )
    {
        var source = NormalizeExtension(sourceExtension);
        var supported = supportedFormats
            ?.Select(NormalizeExtension)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (supported.Count == 0)
        {
            return source;
        }

        if (supported.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            return source;
        }

        if (supported.Contains("webp", StringComparer.OrdinalIgnoreCase))
        {
            return "webp";
        }

        if (supported.Contains("jpg", StringComparer.OrdinalIgnoreCase))
        {
            return "jpg";
        }

        if (supported.Contains("jpeg", StringComparer.OrdinalIgnoreCase))
        {
            return "jpg";
        }

        return source;
    }

    private static bool IsAudio(MetadataType type)
    {
        return type is MetadataType.AlbumReleaseGroup
            or MetadataType.AlbumRelease
            or MetadataType.AlbumMedium
            or MetadataType.Track
            or MetadataType.Recording
            or MetadataType.AudioWork;
    }

    private static bool IsPhoto(MetadataType type)
    {
        return type is MetadataType.PhotoAlbum
            or MetadataType.Photo
            or MetadataType.PictureSet
            or MetadataType.Picture;
    }
}
