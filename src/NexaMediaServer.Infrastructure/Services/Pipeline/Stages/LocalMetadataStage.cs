// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Enums;
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
    private readonly ILogger<LocalMetadataStage> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalMetadataStage"/> class.
    /// </summary>
    /// <param name="partsRegistry">Registry of parsers and extractors.</param>
    /// <param name="logger">Typed logger.</param>
    public LocalMetadataStage(IPartsRegistry partsRegistry, ILogger<LocalMetadataStage> logger)
    {
        this.partsRegistry = partsRegistry;
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

            var hints = item.Hints;
            var updated = item;

            var sidecar = await this.ParseSidecarAsync(
                item.File,
                context.LibrarySection.Type,
                cancellationToken
            );
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

            yield return updated;
        }
    }

    private async Task<SidecarParseResult?> ParseSidecarAsync(
        FileSystemMetadata mediaFile,
        LibraryType libraryType,
        CancellationToken cancellationToken
    )
    {
        if (this.partsRegistry.SidecarParsers.Count == 0)
        {
            return null;
        }

        foreach (var sidecarFile in EnumerateSidecarCandidates(mediaFile))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await this.TryParseSidecarCandidateAsync(
                mediaFile,
                sidecarFile,
                libraryType,
                cancellationToken
            );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<SidecarParseResult?> TryParseSidecarCandidateAsync(
        FileSystemMetadata mediaFile,
        FileSystemMetadata sidecarFile,
        LibraryType libraryType,
        CancellationToken cancellationToken
    )
    {
        foreach (var parser in this.partsRegistry.SidecarParsers)
        {
            if (!parser.CanParse(sidecarFile))
            {
                continue;
            }

            try
            {
                var result = await parser.ParseAsync(
                    new SidecarParseRequest(mediaFile, sidecarFile, libraryType),
                    cancellationToken
                );

                if (result != null)
                {
                    LogSidecarParsed(this.logger, sidecarFile.Path, parser.GetType().Name);
                    return result;
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

        return null;
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
                var result = await extractor.ExtractAsync(
                    new EmbeddedMetadataRequest(mediaFile, libraryType),
                    cancellationToken
                );

                if (result != null)
                {
                    LogEmbeddedExtracted(this.logger, mediaFile.Path, extractor.GetType().Name);
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
}
