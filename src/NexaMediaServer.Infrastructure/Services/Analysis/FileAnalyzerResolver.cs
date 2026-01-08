// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Services.Parts;

namespace NexaMediaServer.Infrastructure.Services.Analysis;

/// <summary>
/// Resolves file analyzers for a metadata item into executable descriptors.
/// </summary>
internal static class FileAnalyzerResolver
{
    /// <summary>
    /// Resolve analyzers for the supplied metadata item.
    /// </summary>
    /// <param name="metadata">Typed metadata DTO.</param>
    /// <param name="partsRegistry">Registry supplying analyzer implementations.</param>
    /// <returns>Ordered analyzer descriptors bound to the provided metadata.</returns>
    internal static AnalyzerDescriptor[] Resolve(
        MetadataBaseItem metadata,
        IPartsRegistry partsRegistry
    )
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(partsRegistry);

        return metadata switch
        {
            Movie movie => CreateAdapters(movie, partsRegistry),
            Show show => CreateAdapters(show, partsRegistry),
            Season season => CreateAdapters(season, partsRegistry),
            Episode episode => CreateAdapters(episode, partsRegistry),
            Trailer trailer => CreateAdapters(trailer, partsRegistry),
            Clip clip => CreateAdapters(clip, partsRegistry),
            Video video => CreateAdapters(video, partsRegistry),
            AlbumReleaseGroup group => CreateAdapters(group, partsRegistry),
            AlbumRelease release => CreateAdapters(release, partsRegistry),
            Track track => CreateAdapters(track, partsRegistry),
            Recording recording => CreateAdapters(recording, partsRegistry),
            AudioWork work => CreateAdapters(work, partsRegistry),
            PhotoAlbum photoAlbum => CreateAdapters(photoAlbum, partsRegistry),
            Photo photo => CreateAdapters(photo, partsRegistry),
            PictureSet pictureSet => CreateAdapters(pictureSet, partsRegistry),
            Picture picture => CreateAdapters(picture, partsRegistry),
            Image image => CreateAdapters(image, partsRegistry),
            _ => Array.Empty<AnalyzerDescriptor>(),
        };
    }

    /// <summary>
    /// Creates analyzer descriptors for a specific metadata type.
    /// </summary>
    private static AnalyzerDescriptor[] CreateAdapters<TMetadata>(
        TMetadata metadata,
        IPartsRegistry partsRegistry
    ) where TMetadata : MetadataBaseItem
    {
        var analyzers = partsRegistry.GetFileAnalyzers<TMetadata>();
        if (analyzers.Count == 0)
        {
            return Array.Empty<AnalyzerDescriptor>();
        }

        return analyzers
            .Select(analyzer => new AnalyzerDescriptor(
                analyzer.Name,
                analyzer.Order,
                item => analyzer.Supports(item, metadata),
                (item, parts, token) => analyzer.AnalyzeAsync(item, metadata, parts, token)
            ))
            .ToArray();
    }

    /// <summary>
    /// A typed descriptor that wraps a concrete file analyzer with pre-bound metadata context.
    /// </summary>
    internal readonly record struct AnalyzerDescriptor(
        string Name,
        int Order,
        Func<MediaItem, bool> Supports,
        Func<MediaItem, IReadOnlyList<MediaPart>, CancellationToken, Task<FileAnalysisResult?>> Analyze
    );
}
