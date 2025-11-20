// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Service responsible for parsing sidecar files (.nfo, metadata.json)
/// and extracting embedded metadata from media files.
/// </summary>
public sealed partial class SidecarMetadataService : ISidecarMetadataService
{
    private readonly IPartsRegistry partsRegistry;
    private readonly IImageOrchestrationService imageOrchestrationService;
    private readonly IContentRatingService contentRatingService;
    private readonly ILogger<SidecarMetadataService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SidecarMetadataService"/> class.
    /// </summary>
    /// <param name="partsRegistry">Registry providing sidecar parsers and embedded extractors.</param>
    /// <param name="imageOrchestrationService">Service for ingesting artwork.</param>
    /// <param name="contentRatingService">Service for resolving content ratings to ages.</param>
    /// <param name="logger">Structured logger for diagnostic output.</param>
    public SidecarMetadataService(
        IPartsRegistry partsRegistry,
        IImageOrchestrationService imageOrchestrationService,
        IContentRatingService contentRatingService,
        ILogger<SidecarMetadataService> logger
    )
    {
        this.partsRegistry = partsRegistry;
        this.imageOrchestrationService = imageOrchestrationService;
        this.contentRatingService = contentRatingService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<SidecarEnrichmentResult> ExtractLocalMetadataAsync(
        MetadataItem item,
        LibrarySection library,
        CancellationToken cancellationToken
    )
    {
        var result = new SidecarEnrichmentResult();

        if (IsExtraMetadata(item.MetadataType))
        {
            this.LogLocalMetadataSkipped(item.Uuid, "Local metadata disabled for extras");
            return result;
        }

        var mediaItems = item.MediaItems ?? new List<MediaItem>();
        var partPaths = mediaItems
            .SelectMany(media => media.Parts ?? Enumerable.Empty<MediaPart>())
            .Select(part => part.File)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (partPaths.Count == 0)
        {
            this.LogLocalMetadataSkipped(item.Uuid, "No media parts with file paths");
            return result;
        }

        foreach (var partPath in partPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mediaFile = FileSystemMetadata.FromPath(partPath);
            if (!mediaFile.Exists || mediaFile.IsDirectory)
            {
                continue;
            }

            this.LogApplyingLocalMetadata(item.Uuid, mediaFile.Path);

            var sidecar = await this.ParseSidecarAsync(mediaFile, library.Type, cancellationToken);
            var embedded = await this.ExtractEmbeddedAsync(
                mediaFile,
                library.Type,
                cancellationToken
            );

            // Ingest artwork from sidecar/embedded (stores files, updates URIs on metadata).
            var sidecarIngested = await this
                .imageOrchestrationService.IngestArtworkAsync(
                    item,
                    sidecar?.Metadata,
                    sidecar?.Source ?? "sidecar",
                    cancellationToken
                )
                .ConfigureAwait(false);

            var embeddedIngested = await this
                .imageOrchestrationService.IngestArtworkAsync(
                    item,
                    embedded?.Metadata,
                    "embedded",
                    cancellationToken
                )
                .ConfigureAwait(false);

            // Collect credits from sidecar.
            if (sidecar?.People is { Count: > 0 })
            {
                result.People ??= new List<PersonCredit>();
                result.People.AddRange(sidecar.People);

                var source = string.IsNullOrWhiteSpace(sidecar.Source) ? "sidecar" : sidecar.Source;
                await this
                    .imageOrchestrationService.IngestCreditArtworkAsync(
                        sidecar.People,
                        null,
                        source,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            if (sidecar?.Groups is { Count: > 0 })
            {
                result.Groups ??= new List<GroupCredit>();
                result.Groups.AddRange(sidecar.Groups);

                var source = string.IsNullOrWhiteSpace(sidecar.Source) ? "sidecar" : sidecar.Source;
                await this
                    .imageOrchestrationService.IngestCreditArtworkAsync(
                        null,
                        sidecar.Groups,
                        source,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            // Collect genres and tags from sidecar.
            if (sidecar?.Genres is { Count: > 0 })
            {
                result.Genres ??= new List<string>();
                result.Genres.AddRange(sidecar.Genres);
            }

            if (sidecar?.Tags is { Count: > 0 })
            {
                result.Tags ??= new List<string>();
                result.Tags.AddRange(sidecar.Tags);
            }

            if (sidecar is null && embedded is null)
            {
                continue;
            }

            // Apply overlay (sidecar > embedded) to item fields.
            var updated =
                this.ApplyLocalMetadata(item, sidecar, embedded)
                || sidecarIngested
                || embeddedIngested;
            if (updated)
            {
                result.LocalMetadataApplied = true;
            }
        }

        return result;
    }

    private static bool IsExtraMetadata(MetadataType metadataType) =>
        metadataType
            is MetadataType.Trailer
                or MetadataType.Clip
                or MetadataType.BehindTheScenes
                or MetadataType.DeletedScene
                or MetadataType.Featurette
                or MetadataType.Interview
                or MetadataType.Scene
                or MetadataType.ShortForm
                or MetadataType.ExtraOther;

    private static IEnumerable<FileSystemMetadata> EnumerateSidecarCandidates(
        FileSystemMetadata mediaFile
    )
    {
        var directoryPath = Path.GetDirectoryName(mediaFile.Path);
        if (string.IsNullOrEmpty(directoryPath))
        {
            yield break;
        }

        IEnumerable<string> candidates;
        try
        {
            candidates = System.IO.Directory.EnumerateFiles(directoryPath);
        }
        catch
        {
            yield break;
        }

        foreach (var candidatePath in candidates)
        {
            if (string.Equals(candidatePath, mediaFile.Path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = FileSystemMetadata.FromPath(candidatePath);
            if (!candidate.Exists || candidate.IsDirectory)
            {
                continue;
            }

            yield return candidate;
        }
    }

    private static string PreferString(string incoming, string current) =>
        string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

    private static string? PreferOptionalString(string? incoming, string? current) =>
        string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

    private static bool AssignIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    /// <summary>
    /// Merges multiple sidecar parse results into a single combined result.
    /// Later results take precedence over earlier ones for metadata fields.
    /// </summary>
    private static SidecarParseResult MergeSidecarResults(List<SidecarParseResult> results)
    {
        if (results.Count == 1)
        {
            return results[0];
        }

        // Merge metadata with later results taking precedence
        MetadataBaseItem? mergedMetadata = null;
        foreach (var metadata in results.Select(r => r.Metadata).Where(m => m is not null))
        {
            if (mergedMetadata is null)
            {
                mergedMetadata = CloneMetadata(metadata!);
            }
            else
            {
                MergeMetadataFields(mergedMetadata, metadata!);
            }
        }

        // Union hints
        IReadOnlyDictionary<string, object>? mergedHints = null;
        foreach (var hints in results.Select(r => r.Hints))
        {
            mergedHints = MergeHints(mergedHints, hints);
        }

        // Union people, groups, genres, tags
        var mergedPeople = MergeCredits(results.Select(r => r.People));
        var mergedGroups = MergeGroupCredits(results.Select(r => r.Groups));
        var mergedGenres = MergeStrings(results.Select(r => r.Genres));
        var mergedTags = MergeStrings(results.Select(r => r.Tags));

        var sources = string.Join("+", results.Select(r => r.Source).Distinct());
        return new SidecarParseResult(
            mergedMetadata,
            mergedHints,
            sources,
            mergedPeople,
            mergedGroups,
            mergedGenres,
            mergedTags
        );
    }

    private static MetadataBaseItem CloneMetadata(MetadataBaseItem source)
    {
        MetadataBaseItem clone = source switch
        {
            Movie => new Movie(),
            Show => new Show(),
            Season => new Season(),
            Episode => new Episode(),
            _ => new MetadataBaseItem(),
        };
        MergeMetadataFields(clone, source);
        return clone;
    }

    private static void MergeMetadataFields(MetadataBaseItem target, MetadataBaseItem source)
    {
        target.Title = PreferString(source.Title, target.Title);
        target.SortTitle = PreferString(source.SortTitle, target.SortTitle);
        target.OriginalTitle = PreferOptionalString(source.OriginalTitle, target.OriginalTitle);
        target.Summary = PreferOptionalString(source.Summary, target.Summary);
        target.Tagline = PreferOptionalString(source.Tagline, target.Tagline);
        target.ContentRating = PreferOptionalString(source.ContentRating, target.ContentRating);
        target.ContentRatingCountryCode = PreferOptionalString(
            source.ContentRatingCountryCode,
            target.ContentRatingCountryCode
        );
        target.ContentRatingAge = source.ContentRatingAge ?? target.ContentRatingAge;
        target.ReleaseDate = source.ReleaseDate ?? target.ReleaseDate;
        target.Year = source.Year ?? target.Year;
        target.Index = source.Index ?? target.Index;
        target.AbsoluteIndex = source.AbsoluteIndex ?? target.AbsoluteIndex;
        target.Duration = source.Duration ?? target.Duration;
        target.ThumbUri = PreferOptionalString(source.ThumbUri, target.ThumbUri);
        target.ThumbHash = PreferOptionalString(source.ThumbHash, target.ThumbHash);
        target.ArtUri = PreferOptionalString(source.ArtUri, target.ArtUri);
        target.ArtHash = PreferOptionalString(source.ArtHash, target.ArtHash);
        target.LogoUri = PreferOptionalString(source.LogoUri, target.LogoUri);
        target.LogoHash = PreferOptionalString(source.LogoHash, target.LogoHash);
    }

    private static IReadOnlyDictionary<string, object>? MergeHints(
        IReadOnlyDictionary<string, object>? existing,
        IReadOnlyDictionary<string, object>? incoming
    )
    {
        if (incoming is null || incoming.Count == 0)
        {
            return existing;
        }

        if (existing is null || existing.Count == 0)
        {
            return incoming;
        }

        var merged = new Dictionary<string, object>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in incoming)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    private static List<PersonCredit>? MergeCredits(
        IEnumerable<IReadOnlyList<PersonCredit>?> sources
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<PersonCredit>();

        foreach (var list in sources)
        {
            if (list is null)
            {
                continue;
            }

            foreach (var credit in list)
            {
                var key = $"{credit.RelationType}:{credit.Person.Title}:{credit.Text}";
                if (seen.Add(key))
                {
                    merged.Add(credit);
                }
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private static List<GroupCredit>? MergeGroupCredits(
        IEnumerable<IReadOnlyList<GroupCredit>?> sources
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<GroupCredit>();

        foreach (var list in sources)
        {
            if (list is null)
            {
                continue;
            }

            foreach (var credit in list)
            {
                var key = $"{credit.RelationType}:{credit.Group.Title}";
                if (seen.Add(key))
                {
                    merged.Add(credit);
                }
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private static List<string>? MergeStrings(IEnumerable<IReadOnlyList<string>?> sources)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var list in sources)
        {
            if (list is null)
            {
                continue;
            }

            foreach (var item in list)
            {
                merged.Add(item);
            }
        }

        return merged.Count > 0 ? merged.ToList() : null;
    }

    private bool ApplyLocalMetadata(
        MetadataItem target,
        SidecarParseResult? sidecar,
        EmbeddedMetadataResult? embedded
    )
    {
        var changed = this.ApplyOverlay(target, embedded?.Metadata);
        changed |= this.ApplyOverlay(target, sidecar?.Metadata);
        return changed;
    }

    private bool ApplyOverlay(MetadataItem target, MetadataBaseItem? overlay)
    {
        if (overlay is null)
        {
            return false;
        }

        var changed = false;

        changed |= AssignIfChanged(
            target.Title,
            PreferString(overlay.Title, target.Title),
            value => target.Title = value
        );

        changed |= AssignIfChanged(
            target.SortTitle,
            PreferString(overlay.SortTitle, target.SortTitle),
            value => target.SortTitle = value
        );

        changed |= AssignIfChanged(
            target.OriginalTitle,
            PreferOptionalString(overlay.OriginalTitle, target.OriginalTitle),
            value => target.OriginalTitle = value
        );

        changed |= AssignIfChanged(
            target.Summary,
            PreferOptionalString(overlay.Summary, target.Summary),
            value => target.Summary = value
        );

        changed |= AssignIfChanged(
            target.Tagline,
            PreferOptionalString(overlay.Tagline, target.Tagline),
            value => target.Tagline = value
        );

        changed |= AssignIfChanged(
            target.ContentRating,
            PreferOptionalString(overlay.ContentRating, target.ContentRating),
            value => target.ContentRating = value
        );

        // Resolve content rating age if rating is provided
        if (!string.IsNullOrWhiteSpace(overlay.ContentRating))
        {
            var isTelevision =
                target.MetadataType
                is MetadataType.Show
                    or MetadataType.Season
                    or MetadataType.Episode;
            var resolvedAge = this.contentRatingService.ResolveAge(
                overlay.ContentRating,
                overlay.ContentRatingCountryCode,
                isTelevision
            );
            changed |= AssignIfChanged(
                target.ContentRatingAge,
                resolvedAge ?? target.ContentRatingAge,
                value => target.ContentRatingAge = value
            );
        }
        else
        {
            changed |= AssignIfChanged(
                target.ContentRatingAge,
                overlay.ContentRatingAge ?? target.ContentRatingAge,
                value => target.ContentRatingAge = value
            );
        }

        changed |= AssignIfChanged(
            target.ReleaseDate,
            overlay.ReleaseDate ?? target.ReleaseDate,
            value => target.ReleaseDate = value
        );

        changed |= AssignIfChanged(
            target.Year,
            overlay.Year ?? target.Year,
            value => target.Year = value
        );

        changed |= AssignIfChanged(
            target.Index,
            overlay.Index ?? target.Index,
            value => target.Index = value
        );

        changed |= AssignIfChanged(
            target.AbsoluteIndex,
            overlay.AbsoluteIndex ?? target.AbsoluteIndex,
            value => target.AbsoluteIndex = value
        );

        changed |= AssignIfChanged(
            target.Duration,
            overlay.Duration ?? target.Duration,
            value => target.Duration = value
        );

        changed |= AssignIfChanged(
            target.ThumbUri,
            PreferOptionalString(overlay.ThumbUri, target.ThumbUri),
            value => target.ThumbUri = value
        );

        changed |= AssignIfChanged(
            target.ThumbHash,
            PreferOptionalString(overlay.ThumbHash, target.ThumbHash),
            value => target.ThumbHash = value
        );

        changed |= AssignIfChanged(
            target.ArtUri,
            PreferOptionalString(overlay.ArtUri, target.ArtUri),
            value => target.ArtUri = value
        );

        changed |= AssignIfChanged(
            target.ArtHash,
            PreferOptionalString(overlay.ArtHash, target.ArtHash),
            value => target.ArtHash = value
        );

        changed |= AssignIfChanged(
            target.LogoUri,
            PreferOptionalString(overlay.LogoUri, target.LogoUri),
            value => target.LogoUri = value
        );

        changed |= AssignIfChanged(
            target.LogoHash,
            PreferOptionalString(overlay.LogoHash, target.LogoHash),
            value => target.LogoHash = value
        );

        return changed;
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

        // Enumerate all candidates once upfront for batch processing
        var candidates = EnumerateSidecarCandidates(mediaFile).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

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
                candidates,
                processedParsers,
                cancellationToken
            );
            results.AddRange(fileResults);
        }

        if (results.Count == 0)
        {
            return null;
        }

        // Merge all results with later results taking precedence
        return MergeSidecarResults(results);
    }

    private async Task<List<SidecarParseResult>> TryParseAllAsync(
        FileSystemMetadata mediaFile,
        FileSystemMetadata sidecarFile,
        LibraryType libraryType,
        IReadOnlyList<FileSystemMetadata> siblings,
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
                    this.LogSidecarParserFinished(
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
                this.LogSidecarParseFailed(sidecarFile.Path, ex);
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
                    this.LogEmbeddedExtractorFinished(
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
                this.LogEmbeddedExtractionFailed(mediaFile.Path, ex);
            }
        }

        return null;
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipped local metadata for {MetadataItemUuid}: {Reason}"
    )]
    private partial void LogLocalMetadataSkipped(Guid metadataItemUuid, string reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applying local metadata for {MetadataItemUuid} from {MediaPath}"
    )]
    private partial void LogApplyingLocalMetadata(Guid metadataItemUuid, string mediaPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Sidecar parser {ParserName} finished for {SidecarPath} in {ElapsedMs}ms"
    )]
    private partial void LogSidecarParserFinished(
        string parserName,
        string sidecarPath,
        long elapsedMs
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sidecar parsing failed for {SidecarPath}")]
    private partial void LogSidecarParseFailed(string sidecarPath, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Embedded extractor {ExtractorName} finished for {MediaPath} in {ElapsedMs}ms"
    )]
    private partial void LogEmbeddedExtractorFinished(
        string extractorName,
        string mediaPath,
        long elapsedMs
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Embedded metadata extraction failed for {MediaPath}"
    )]
    private partial void LogEmbeddedExtractionFailed(string mediaPath, Exception ex);
    #endregion
}
