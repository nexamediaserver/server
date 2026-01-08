// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Music;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Helper utilities for <see cref="LocalMetadataStage"/>.
/// </summary>
public sealed partial class LocalMetadataStage
{
    /// <summary>
    /// Merges multiple sidecar parse results into a single combined result.
    /// Later results take precedence over earlier ones for metadata fields.
    /// Collections (people, groups, genres, tags) are unioned.
    /// </summary>
    private static SidecarParseResult MergeSidecarResults(List<SidecarParseResult> results)
    {
        if (results.Count == 1)
        {
            return results[0];
        }

        // Merge metadata with later results taking precedence (last-write-wins for non-null fields)
        MetadataBaseItem? mergedMetadata = null;
        foreach (var metadata in results.Select(r => r.Metadata).Where(m => m is not null))
        {
            if (mergedMetadata is null)
            {
                // Clone the first metadata to avoid mutating original
                mergedMetadata = CloneMetadata(metadata!);
            }
            else
            {
                MergeMetadataFields(mergedMetadata, metadata!);
            }
        }

        // Union hints (later values overwrite)
        IReadOnlyDictionary<string, object>? mergedHints = null;
        foreach (var hints in results.Select(r => r.Hints))
        {
            mergedHints = MergeHints(mergedHints, hints);
        }

        // Union people, groups, genres, tags (deduped)
        var mergedPeople = MergeCredits(results.Select(r => r.People));
        var mergedGroups = MergeGroupCredits(results.Select(r => r.Groups));
        var mergedGenres = MergeStrings(results.Select(r => r.Genres));
        var mergedTags = MergeStrings(results.Select(r => r.Tags));

        // Source indicates this is a merged result
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
        // Create appropriate subtype to preserve type information
        MetadataBaseItem clone = source switch
        {
            Movie => new Movie(),
            Show => new Show(),
            Season => new Season(),
            Episode => new Episode(),
            Track => new Track(),
            Recording => new Recording(),
            AlbumRelease => new AlbumRelease(),
            AlbumReleaseGroup => new AlbumReleaseGroup(),
            AlbumMedium => new AlbumMedium(),
            AudioWork => new AudioWork(),
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

        // Year is derived from ReleaseDate when ReleaseDate is provided
        // If both are provided and mismatch, prefer year from ReleaseDate
        var yearToAssign = source.Year ?? target.Year;
        if (source.ReleaseDate.HasValue)
        {
            // Always derive from ReleaseDate when it's available
            yearToAssign = source.ReleaseDate.Value.Year;
        }

        target.Year = yearToAssign;
        target.Index = source.Index ?? target.Index;
        target.AbsoluteIndex = source.AbsoluteIndex ?? target.AbsoluteIndex;
        target.Duration = source.Duration ?? target.Duration;
        target.ThumbUri = PreferOptionalString(source.ThumbUri, target.ThumbUri);
        target.ThumbHash = PreferOptionalString(source.ThumbHash, target.ThumbHash);
        target.ArtUri = PreferOptionalString(source.ArtUri, target.ArtUri);
        target.ArtHash = PreferOptionalString(source.ArtHash, target.ArtHash);
        target.LogoUri = PreferOptionalString(source.LogoUri, target.LogoUri);
        target.LogoHash = PreferOptionalString(source.LogoHash, target.LogoHash);

        // Merge ExtraFields (source values take precedence)
        foreach (var kvp in source.ExtraFields)
        {
            target.ExtraFields[kvp.Key] = kvp.Value;
        }

        // Merge PendingExternalIds (union)
        foreach (var externalId in source.PendingExternalIds.Where(e => !target.PendingExternalIds.Contains(e)))
        {
            target.PendingExternalIds.Add(externalId);
        }
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

    private static bool IsExtraMetadata(MetadataBaseItem metadata) =>
        metadata
            is Trailer
                or Clip
                or BehindTheScenes
                or DeletedScene
                or Featurette
                or Interview
                or Scene
                or ShortForm
                or ExtraOther;

    private static string PreferString(string incoming, string current) =>
        string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

    private static string? PreferOptionalString(string? incoming, string? current) =>
        string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

    /// <summary>
    /// Enumerates sidecar candidates from pre-fetched siblings, avoiding directory re-enumeration.
    /// </summary>
    private static IEnumerable<FileSystemMetadata> EnumerateSidecarCandidatesFromSiblings(
        FileSystemMetadata mediaFile,
        IReadOnlyList<FileSystemMetadata> siblings
    )
    {
        foreach (var sibling in siblings)
        {
            if (sibling.IsDirectory)
            {
                continue;
            }

            if (string.Equals(sibling.Path, mediaFile.Path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return sibling;
        }
    }

    /// <summary>
    /// Fallback: enumerates sidecar candidates by scanning the directory (used when siblings are not available).
    /// </summary>
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
            candidates = Directory.EnumerateFiles(directoryPath);
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

    /// <summary>
    /// Applies music-specific hints from embedded/sidecar metadata to the resolved item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method processes MusicBrainz Picard tags and other music metadata, mapping them
    /// to the item's ExtraFields (for type-specific data like release type, BPM, classical
    /// music work information) and PendingExternalIds (for ISRCs, barcodes, MBIDs).
    /// </para>
    /// </remarks>
    private static void ApplyMusicHints(
        MetadataBaseItem resolved,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        // Apply type-specific hints
        switch (resolved)
        {
            case Track track:
                MusicHintsProcessor.ApplyHintsToTrack(track, hints);
                break;

            case Recording recording:
                // Recording shares track-level hints - apply to MetadataBaseItem
                ApplyTrackHintsToItem(recording, hints);
                break;

            case AlbumRelease release:
                MusicHintsProcessor.ApplyHintsToAlbumRelease(release, hints);
                break;

            case AlbumReleaseGroup releaseGroup:
                MusicHintsProcessor.ApplyHintsToAlbumReleaseGroup(releaseGroup, hints);
                break;

            case AlbumMedium medium:
                MusicHintsProcessor.ApplyHintsToAlbumMedium(medium, hints);
                break;

            case AudioWork work:
                MusicHintsProcessor.ApplyHintsToAudioWork(work, hints);
                break;
        }
    }

    /// <summary>
    /// Applies track-level hints to any MetadataBaseItem (used for Recording which shares Track's hint structure).
    /// </summary>
    private static void ApplyTrackHintsToItem(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object>? hints)
    {
        // Create a temporary Track to extract hints, then copy to the item
        var tempTrack = new Track();
        MusicHintsProcessor.ApplyHintsToTrack(tempTrack, hints);

        // Copy ExtraFields from temp to target
        foreach (var kvp in tempTrack.ExtraFields)
        {
            item.ExtraFields[kvp.Key] = kvp.Value;
        }

        // Copy PendingExternalIds (avoiding duplicates)
        foreach (var extId in tempTrack.PendingExternalIds.Where(e => !item.PendingExternalIds.Contains(e)))
        {
            item.PendingExternalIds.Add(extId);
        }
    }

    private MetadataBaseItem ApplyLocalMetadata(
        MetadataBaseItem resolved,
        SidecarParseResult? sidecar,
        EmbeddedMetadataResult? embedded
    )
    {
        this.ApplyOverlay(resolved, embedded?.Metadata);
        this.ApplyOverlay(resolved, sidecar?.Metadata);

        // Apply music-specific hints to ExtraFields and PendingExternalIds
        var mergedHints = MergeHints(embedded?.Hints, sidecar?.Hints);
        ApplyMusicHints(resolved, mergedHints);

        return resolved;
    }

    private void ApplyOverlay(MetadataBaseItem target, MetadataBaseItem? overlay)
    {
        if (overlay is null)
        {
            return;
        }

        if (
            !target.GetType().IsInstanceOfType(overlay)
            && !overlay.GetType().IsInstanceOfType(target)
        )
        {
            return;
        }

        target.Title = PreferString(overlay.Title, target.Title);
        target.SortTitle = PreferString(overlay.SortTitle, target.SortTitle);
        target.OriginalTitle = PreferOptionalString(overlay.OriginalTitle, target.OriginalTitle);
        target.Summary = PreferOptionalString(overlay.Summary, target.Summary);
        target.Tagline = PreferOptionalString(overlay.Tagline, target.Tagline);
        target.ContentRating = PreferOptionalString(overlay.ContentRating, target.ContentRating);

        // Resolve content rating age if rating is provided
        if (!string.IsNullOrWhiteSpace(overlay.ContentRating))
        {
            var isTelevision = target is Show or Season or Episode;
            var resolvedAge = this.contentRatingService.ResolveAge(
                overlay.ContentRating,
                overlay.ContentRatingCountryCode,
                isTelevision
            );
            target.ContentRatingAge = resolvedAge ?? target.ContentRatingAge;
        }
        else
        {
            target.ContentRatingAge = overlay.ContentRatingAge ?? target.ContentRatingAge;
        }

        target.ReleaseDate = overlay.ReleaseDate ?? target.ReleaseDate;

        // Year is derived from ReleaseDate when ReleaseDate is provided
        // If both are provided and mismatch, prefer year from ReleaseDate
        var yearToAssign = overlay.Year ?? target.Year;
        if (overlay.ReleaseDate.HasValue)
        {
            // Always derive from ReleaseDate when it's available
            yearToAssign = overlay.ReleaseDate.Value.Year;
        }

        target.Year = yearToAssign;
        target.Index = overlay.Index ?? target.Index;
        target.AbsoluteIndex = overlay.AbsoluteIndex ?? target.AbsoluteIndex;
        target.Duration = overlay.Duration ?? target.Duration;
        target.ThumbUri = PreferOptionalString(overlay.ThumbUri, target.ThumbUri);
        target.ThumbHash = PreferOptionalString(overlay.ThumbHash, target.ThumbHash);
        target.ArtUri = PreferOptionalString(overlay.ArtUri, target.ArtUri);
        target.ArtHash = PreferOptionalString(overlay.ArtHash, target.ArtHash);
        target.LogoUri = PreferOptionalString(overlay.LogoUri, target.LogoUri);
        target.LogoHash = PreferOptionalString(overlay.LogoHash, target.LogoHash);
    }
}
