// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Music;

/// <summary>
/// Processes embedded music metadata hints and maps them to DTO properties,
/// ExtraFields, and ExternalIdentifiers.
/// </summary>
/// <remarks>
/// <para>
/// This class transforms the raw hint dictionary produced by <c>TagLibMetadataExtractor</c>
/// into structured data that can be persisted to the database. It handles MusicBrainz Picard
/// tags including basic metadata, release information, credits, and classical music fields.
/// </para>
/// <para>
/// The processor is stateless and can be used concurrently from multiple threads.
/// </para>
/// </remarks>
public static class MusicHintsProcessor
{
    /// <summary>
    /// Applies music hints to a Track DTO, populating ExtraFields for music-specific data
    /// and PendingExternalIds for external identifiers.
    /// </summary>
    /// <param name="track">The track DTO to update.</param>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    public static void ApplyHintsToTrack(
        Track track,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        // Apply sort names
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.TitleSort, ExtraFieldKeys.TitleSort);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.ArtistSort, ExtraFieldKeys.ArtistSort);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.AlbumArtistSort, ExtraFieldKeys.AlbumArtistSort);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.ComposerSort, ExtraFieldKeys.ComposerSort);

        // Apply recording information
        SetExtraFieldIfPresentInt(track, hints, EmbeddedMetadataHintKeys.Bpm, ExtraFieldKeys.Bpm);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Key, ExtraFieldKeys.Key);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Copyright, ExtraFieldKeys.Copyright);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Lyrics, ExtraFieldKeys.Lyrics);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Comment, ExtraFieldKeys.Comment);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.License, ExtraFieldKeys.License);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Language, ExtraFieldKeys.Language);

        // Classical music fields
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Work, ExtraFieldKeys.WorkTitle);
        SetExtraFieldIfPresent(track, hints, EmbeddedMetadataHintKeys.Movement, ExtraFieldKeys.MovementTitle);
        SetExtraFieldIfPresentInt(track, hints, EmbeddedMetadataHintKeys.MovementNumber, ExtraFieldKeys.MovementNumber);
        SetExtraFieldIfPresentInt(track, hints, EmbeddedMetadataHintKeys.MovementTotal, ExtraFieldKeys.MovementTotal);
        SetExtraFieldIfPresentBool(track, hints, EmbeddedMetadataHintKeys.ShowMovement, ExtraFieldKeys.ShowMovement);

        // Extract external IDs
        AddExternalIdIfPresent(track, hints, EmbeddedMetadataHintKeys.MusicBrainzTrackId, ExternalIdProviders.MusicBrainzTrack);
        AddExternalIdIfPresent(track, hints, EmbeddedMetadataHintKeys.MusicBrainzRecordingId, ExternalIdProviders.MusicBrainzRecording);
        AddExternalIdIfPresent(track, hints, EmbeddedMetadataHintKeys.Isrc, ExternalIdProviders.Isrc);
        AddExternalIdIfPresent(track, hints, EmbeddedMetadataHintKeys.AcoustId, ExternalIdProviders.AcoustId);

        // Also check the external_ids dictionary
        AddExternalIdsFromDictionary(track, hints, EmbeddedMetadataHintKeys.ExternalIds);
    }

    /// <summary>
    /// Applies music hints to an AlbumRelease DTO.
    /// </summary>
    /// <param name="release">The album release DTO to update.</param>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    public static void ApplyHintsToAlbumRelease(
        AlbumRelease release,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        // Release information
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.ReleaseType, ExtraFieldKeys.ReleaseType);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.ReleaseStatus, ExtraFieldKeys.ReleaseStatus);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.ReleaseCountry, ExtraFieldKeys.ReleaseCountry);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.CatalogNumber, ExtraFieldKeys.CatalogNumber);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.Script, ExtraFieldKeys.Script);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.OriginalDate, ExtraFieldKeys.OriginalDate);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.AlbumSort, ExtraFieldKeys.AlbumSort);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.Label, ExtraFieldKeys.LabelName);
        SetExtraFieldIfPresentBool(release, hints, EmbeddedMetadataHintKeys.Compilation, ExtraFieldKeys.Compilation);
        SetExtraFieldIfPresent(release, hints, EmbeddedMetadataHintKeys.Website, ExtraFieldKeys.Website);
        SetExtraFieldIfPresentInt(release, hints, EmbeddedMetadataHintKeys.OriginalYear, ExtraFieldKeys.OriginalYear);

        // Extract external IDs
        AddExternalIdIfPresent(release, hints, EmbeddedMetadataHintKeys.MusicBrainzReleaseId, ExternalIdProviders.MusicBrainzRelease);
        AddExternalIdIfPresent(release, hints, EmbeddedMetadataHintKeys.Barcode, ExternalIdProviders.Barcode);
        AddExternalIdIfPresent(release, hints, EmbeddedMetadataHintKeys.AmazonAsin, ExternalIdProviders.Amazon);

        // Check external_ids dictionary
        AddExternalIdsFromDictionary(
            release,
            hints,
            EmbeddedMetadataHintKeys.ExternalIds,
            ExternalIdProviders.MusicBrainzRelease,
            ExternalIdProviders.Barcode,
            ExternalIdProviders.Amazon,
            ExternalIdProviders.MusicBrainzDiscId,
            ExternalIdProviders.DiscogsRelease);
    }

    /// <summary>
    /// Applies music hints to an AlbumReleaseGroup DTO.
    /// </summary>
    /// <param name="releaseGroup">The album release group DTO to update.</param>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    public static void ApplyHintsToAlbumReleaseGroup(
        AlbumReleaseGroup releaseGroup,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        // Release type applies to release group too
        SetExtraFieldIfPresent(releaseGroup, hints, EmbeddedMetadataHintKeys.ReleaseType, ExtraFieldKeys.ReleaseType);

        // Check external_ids dictionary
        AddExternalIdsFromDictionary(
            releaseGroup,
            hints,
            EmbeddedMetadataHintKeys.ExternalIds,
            ExternalIdProviders.MusicBrainzReleaseGroup,
            ExternalIdProviders.DiscogsMaster);
    }

    /// <summary>
    /// Applies music hints to an AlbumMedium DTO.
    /// </summary>
    /// <param name="medium">The album medium DTO to update.</param>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    public static void ApplyHintsToAlbumMedium(
        AlbumMedium medium,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        SetExtraFieldIfPresent(medium, hints, EmbeddedMetadataHintKeys.Media, ExtraFieldKeys.MediaFormat);
        SetExtraFieldIfPresent(medium, hints, EmbeddedMetadataHintKeys.DiscSubtitle, ExtraFieldKeys.DiscSubtitle);
    }

    /// <summary>
    /// Applies music hints to an AudioWork DTO.
    /// </summary>
    /// <param name="work">The audio work DTO to update.</param>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    public static void ApplyHintsToAudioWork(
        AudioWork work,
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return;
        }

        // Work title (use as Title if not set)
        if (hints.TryGetValue(EmbeddedMetadataHintKeys.Work, out var workTitle))
        {
            var title = workTitle?.ToString();
            if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(work.Title))
            {
                work.Title = title.Trim();
            }
        }

        // Movement information
        SetExtraFieldIfPresent(work, hints, EmbeddedMetadataHintKeys.Movement, ExtraFieldKeys.MovementTitle);
        SetExtraFieldIfPresentInt(work, hints, EmbeddedMetadataHintKeys.MovementNumber, ExtraFieldKeys.MovementNumber);
        SetExtraFieldIfPresentInt(work, hints, EmbeddedMetadataHintKeys.MovementTotal, ExtraFieldKeys.MovementTotal);
        SetExtraFieldIfPresentBool(work, hints, EmbeddedMetadataHintKeys.ShowMovement, ExtraFieldKeys.ShowMovement);
        SetExtraFieldIfPresent(work, hints, EmbeddedMetadataHintKeys.Language, ExtraFieldKeys.Language);

        // Check external_ids dictionary for work ID
        AddExternalIdsFromDictionary(work, hints, EmbeddedMetadataHintKeys.ExternalIds, ExternalIdProviders.MusicBrainzWork);

        // Also check direct hint
        AddExternalIdIfPresent(work, hints, EmbeddedMetadataHintKeys.MusicBrainzWorkId, ExternalIdProviders.MusicBrainzWork);
    }

    /// <summary>
    /// Extracts person credits from music hints.
    /// </summary>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    /// <returns>List of person credits extracted from the hints.</returns>
    public static IReadOnlyList<PersonCredit> ExtractPersonCredits(
        IReadOnlyDictionary<string, object>? hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return [];
        }

        var credits = new List<PersonCredit>();

        // Composers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Composers, RelationType.PersonComposesAudio, null);

        // Conductors
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Conductors, RelationType.PersonConductsAudio, null);

        // Lyricists
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Lyricists, RelationType.PersonWritesLyricsForAudio, null);

        // Arrangers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Arrangers, RelationType.PersonArrangesAudio, null);

        // Producers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Producers, RelationType.PersonProducesAudio, null);

        // Engineers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Engineers, RelationType.PersonEngineersAudio, null);

        // Mixers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Mixers, RelationType.PersonMixesAudio, null);

        // DJ Mixers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.DjMixers, RelationType.PersonDjMixesAudio, null);

        // Remixers
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Remixers, RelationType.PersonRemixesAudio, null);

        // Directors
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Directors, RelationType.PersonDirectsAudio, null);

        // Writers (general songwriters - treat as composers)
        AddCreditsFromHint(credits, hints, EmbeddedMetadataHintKeys.Writers, RelationType.PersonComposesAudio, "writer");

        // Performer credits with specific roles
        AddPerformerCreditsWithRoles(credits, hints);

        return credits;
    }

    /// <summary>
    /// Extracts artist external IDs from hints (multi-value support).
    /// </summary>
    /// <param name="hints">The hints dictionary from embedded metadata extraction.</param>
    /// <returns>Dictionary mapping artist names to their external IDs.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<(string Provider, string Id)>> ExtractArtistExternalIds(
        IReadOnlyDictionary<string, object>? hints)
    {
        var result = new Dictionary<string, IReadOnlyList<(string Provider, string Id)>>(StringComparer.OrdinalIgnoreCase);

        if (hints is null)
        {
            return result;
        }

        // Extract performer names with their MusicBrainz IDs
        var performers = GetStringList(hints, EmbeddedMetadataHintKeys.Performers);
        var artistIds = GetStringList(hints, EmbeddedMetadataHintKeys.ArtistExternalIds, ExternalIdProviders.MusicBrainzArtist);

        // Match performers with their IDs (same order)
        for (var i = 0; i < performers.Count && i < artistIds.Count; i++)
        {
            var name = performers[i];
            var id = artistIds[i];
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
            {
                result[name] = [(ExternalIdProviders.MusicBrainzArtist, id)];
            }
        }

        // Do the same for album artists
        var albumArtists = GetStringList(hints, EmbeddedMetadataHintKeys.AlbumArtists);
        var albumArtistIds = GetStringList(hints, EmbeddedMetadataHintKeys.AlbumArtistExternalIds, ExternalIdProviders.MusicBrainzReleaseArtist);

        for (var i = 0; i < albumArtists.Count && i < albumArtistIds.Count; i++)
        {
            var name = albumArtists[i];
            var id = albumArtistIds[i];
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
            {
                if (result.TryGetValue(name, out var existing))
                {
                    // Merge IDs
                    var merged = existing.ToList();
                    merged.Add((ExternalIdProviders.MusicBrainzReleaseArtist, id));
                    result[name] = merged;
                }
                else
                {
                    result[name] = [(ExternalIdProviders.MusicBrainzReleaseArtist, id)];
                }
            }
        }

        return result;
    }

    private static void SetExtraFieldIfPresent(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        string extraFieldKey)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        var str = value?.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return;
        }

        item.ExtraFields[extraFieldKey] = str.Trim();
    }

    private static void SetExtraFieldIfPresentInt(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        string extraFieldKey)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        var str = value?.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return;
        }

        if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
        {
            item.ExtraFields[extraFieldKey] = intVal;
        }
    }

    private static void SetExtraFieldIfPresentBool(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        string extraFieldKey)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        var str = value?.ToString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return;
        }

        var boolVal = str.Trim().ToUpperInvariant() is "1" or "TRUE" or "YES";
        item.ExtraFields[extraFieldKey] = boolVal;
    }

    private static void AddExternalIdIfPresent(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        string provider)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        var str = value?.ToString();
        if (!string.IsNullOrWhiteSpace(str))
        {
            var trimmed = str.Trim();
            var entry = (provider, trimmed);
            if (!item.PendingExternalIds.Contains(entry))
            {
                item.PendingExternalIds.Add(entry);
            }
        }
    }

    private static void AddExternalIdsFromDictionary(
        MetadataBaseItem item,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        params string[] providersToInclude)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        if (value is not IDictionary<string, string> idsDict)
        {
            return;
        }

        var providerSet = providersToInclude.Length > 0
            ? new HashSet<string>(providersToInclude, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var (provider, id) in idsDict)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (providerSet is not null && !providerSet.Contains(provider))
            {
                continue;
            }

            var trimmed = id.Trim();
            var entry = (provider, trimmed);
            if (!item.PendingExternalIds.Contains(entry))
            {
                item.PendingExternalIds.Add(entry);
            }
        }
    }

    private static void AddCreditsFromHint(
        List<PersonCredit> credits,
        IReadOnlyDictionary<string, object> hints,
        string hintKey,
        RelationType relationType,
        string? role)
    {
        if (!hints.TryGetValue(hintKey, out var value))
        {
            return;
        }

        var names = ExtractNames(value);
        foreach (var name in names)
        {
            credits.Add(new PersonCredit(new Person { Title = name }, relationType, role));
        }
    }

    private static void AddPerformerCreditsWithRoles(
        List<PersonCredit> credits,
        IReadOnlyDictionary<string, object> hints)
    {
        if (!hints.TryGetValue(EmbeddedMetadataHintKeys.PerformerCredits, out var performerCreditsObj) ||
            performerCreditsObj is not IDictionary<string, object> performerCredits)
        {
            return;
        }

        foreach (var (role, namesObj) in performerCredits)
        {
            var names = ExtractNames(namesObj);
            foreach (var name in names)
            {
                credits.Add(new PersonCredit(
                    new Person { Title = name },
                    RelationType.PersonPerformsInstrumentOrVocals,
                    role));
            }
        }
    }

    private static IEnumerable<string> ExtractNames(object? value)
    {
        if (value is string singleValue && !string.IsNullOrWhiteSpace(singleValue))
        {
            // Single value - may be semicolon-separated
            foreach (var name in singleValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return name;
            }
        }
        else if (value is IEnumerable<string> strings)
        {
            foreach (var name in strings.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                yield return name.Trim();
            }
        }
        else if (value is IEnumerable<object> objects)
        {
            foreach (var nameObj in objects)
            {
                var name = nameObj?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return name.Trim();
                }
            }
        }
    }

    private static List<string> GetStringList(IReadOnlyDictionary<string, object> hints, string key, string? subKey = null)
    {
        if (!hints.TryGetValue(key, out var value))
        {
            return [];
        }

        if (subKey is not null && value is IDictionary<string, object> dict && !dict.TryGetValue(subKey, out value))
        {
            return [];
        }

        if (value is IEnumerable<string> strings)
        {
            return strings.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        }

        if (value is IEnumerable<object> objects)
        {
            return objects
                .Select(o => o?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToList();
        }

        var str = value?.ToString();
        if (!string.IsNullOrWhiteSpace(str))
        {
            return [str.Trim()];
        }

        return [];
    }
}
