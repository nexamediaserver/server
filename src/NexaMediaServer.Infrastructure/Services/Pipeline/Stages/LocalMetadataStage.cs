// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Extracts local metadata from sidecar files and embedded tags.
/// </summary>
public sealed partial class LocalMetadataStage : IScanPipelineStage<ScanWorkItem, ScanWorkItem>
{
    private readonly IPartsRegistry partsRegistry;
    private readonly IImageService imageService;
    private readonly IContentRatingService contentRatingService;
    private readonly ILogger<LocalMetadataStage> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalMetadataStage"/> class.
    /// </summary>
    /// <param name="partsRegistry">Registry of parsers and extractors.</param>
    /// <param name="imageService">Image ingestion service.</param>
    /// <param name="contentRatingService">Service for resolving content ratings to ages.</param>
    /// <param name="logger">Typed logger.</param>
    public LocalMetadataStage(
        IPartsRegistry partsRegistry,
        IImageService imageService,
        IContentRatingService contentRatingService,
        ILogger<LocalMetadataStage> logger
    )
    {
        this.partsRegistry = partsRegistry;
        this.imageService = imageService;
        this.contentRatingService = contentRatingService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public string Name => "local_metadata";

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<ScanWorkItem> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var item in input.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.File.IsDirectory)
            {
                yield return item;
                continue;
            }

            if (item.ResolvedMetadata is { } resolved && IsExtraMetadata(resolved))
            {
                yield return item;
                continue;
            }

            var hints = item.Hints;
            var updated = item;

            var sidecar = await this.ParseSidecarAsync(
                item.File,
                item.Siblings,
                context.LibrarySection.Type,
                cancellationToken
            );
            if (sidecar != null && updated.ResolvedMetadata != null)
            {
                sidecar = await this.IngestSidecarArtworkAsync(
                        updated.ResolvedMetadata,
                        sidecar,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            if (sidecar != null)
            {
                hints = MergeHints(hints, sidecar.Hints);
                updated = updated with { Sidecar = sidecar, Hints = hints };
            }

            var embedded = await this.ExtractEmbeddedAsync(
                item.File,
                context.LibrarySection.Type,
                cancellationToken
            );
            if (embedded != null)
            {
                hints = MergeHints(hints, embedded.Hints);
                updated = updated with { Embedded = embedded, Hints = hints };
            }

            if (updated.ResolvedMetadata != null)
            {
                var merged = this.ApplyLocalMetadata(updated.ResolvedMetadata, sidecar, embedded);
                updated = updated with { ResolvedMetadata = merged };
            }

            yield return updated;
        }
    }

    private async Task<SidecarParseResult?> ParseSidecarAsync(
        FileSystemMetadata mediaFile,
        IReadOnlyList<FileSystemMetadata>? siblings,
        LibraryType libraryType,
        CancellationToken cancellationToken
    )
    {
        if (this.partsRegistry.SidecarParsers.Count == 0)
        {
            return null;
        }

        // Use pre-enumerated siblings when available to avoid re-scanning the directory
        var candidates = (
            siblings != null
                ? EnumerateSidecarCandidatesFromSiblings(mediaFile, siblings)
                : EnumerateSidecarCandidates(mediaFile)
        ).ToList();

        // Collect results from ALL parsers instead of early-exit
        var results = new List<SidecarParseResult>();
        var processedParsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sidecarFile in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileResults = await this.TryParseAllAsync(
                mediaFile,
                sidecarFile,
                libraryType,
                siblings,
                processedParsers,
                cancellationToken
            );
            results.AddRange(fileResults);
        }

        if (results.Count == 0)
        {
            return null;
        }

        // Merge all results with later results taking precedence (local-artwork runs last)
        return MergeSidecarResults(results);
    }

    private async Task<List<SidecarParseResult>> TryParseAllAsync(
        FileSystemMetadata mediaFile,
        FileSystemMetadata sidecarFile,
        LibraryType libraryType,
        IReadOnlyList<FileSystemMetadata>? siblings,
        HashSet<string> processedParsers,
        CancellationToken cancellationToken
    )
    {
        var results = new List<SidecarParseResult>();

        foreach (var parser in this.partsRegistry.SidecarParsers)
        {
            // Skip parsers we've already processed successfully for this media file
            if (processedParsers.Contains(parser.Name))
            {
                continue;
            }

            if (!parser.CanParse(sidecarFile))
            {
                continue;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var request = new SidecarParseRequest(
                    mediaFile,
                    sidecarFile,
                    libraryType,
                    siblings
                );
                var result = await parser.ParseAsync(request, cancellationToken);

                if (result != null)
                {
                    LogSidecarParserFinished(
                        this.logger,
                        parser.Name,
                        sidecarFile.Path,
                        sw.ElapsedMilliseconds
                    );
                    results.Add(result);
                    processedParsers.Add(parser.Name);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogSidecarParseFailed(this.logger, sidecarFile.Path, ex);
            }
        }

        return results;
    }

    private async Task<EmbeddedMetadataResult?> ExtractEmbeddedAsync(
        FileSystemMetadata mediaFile,
        LibraryType libraryType,
        CancellationToken cancellationToken
    )
    {
        if (this.partsRegistry.EmbeddedMetadataExtractors.Count == 0)
        {
            return null;
        }

        foreach (var extractor in this.partsRegistry.EmbeddedMetadataExtractors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!extractor.CanExtract(mediaFile))
            {
                continue;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var result = await extractor.ExtractAsync(
                    new EmbeddedMetadataRequest(mediaFile, libraryType),
                    cancellationToken
                );

                if (result != null)
                {
                    LogEmbeddedExtractorFinished(
                        this.logger,
                        extractor.Name,
                        mediaFile.Path,
                        sw.ElapsedMilliseconds
                    );
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogEmbeddedExtractionFailed(this.logger, mediaFile.Path, ex);
            }
        }

        return null;
    }

    private async Task<SidecarParseResult> IngestSidecarArtworkAsync(
        MetadataBaseItem resolved,
        SidecarParseResult sidecar,
        CancellationToken cancellationToken
    )
    {
        if (sidecar.Metadata is null)
        {
            return sidecar;
        }

        var sourceIdentifier = string.IsNullOrWhiteSpace(sidecar.Source)
            ? "sidecar"
            : sidecar.Source;

        // Use the resolved metadata item's UUID as the filesystem anchor for artwork.
        var owner = new MetadataItem { Uuid = resolved.Uuid };
        var metadata = sidecar.Metadata;

        async Task<string?> IngestAsync(string? uri, ArtworkKind kind)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return uri;
            }

            return await this
                .imageService.IngestExternalArtworkAsync(
                    owner,
                    sourceIdentifier,
                    kind,
                    uri,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        var thumb = await IngestAsync(metadata.ThumbUri, ArtworkKind.Poster).ConfigureAwait(false);
        if (thumb != metadata.ThumbUri)
        {
            metadata.ThumbUri = thumb;
        }

        var art = await IngestAsync(metadata.ArtUri, ArtworkKind.Backdrop).ConfigureAwait(false);
        if (art != metadata.ArtUri)
        {
            metadata.ArtUri = art;
        }

        var logo = await IngestAsync(metadata.LogoUri, ArtworkKind.Logo).ConfigureAwait(false);
        if (logo != metadata.LogoUri)
        {
            metadata.LogoUri = logo;
        }

        return sidecar;
    }
}
