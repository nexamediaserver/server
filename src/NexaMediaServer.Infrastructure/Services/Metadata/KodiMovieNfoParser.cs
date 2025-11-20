// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Parses Kodi-style movie NFO files (<see href="https://kodi.wiki/view/NFO_files/Movies" />).
/// Produces <see cref="Movie" /> metadata overrides and external identifier hints.
/// </summary>
public sealed class KodiMovieNfoParser : ISidecarParser
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyy.MM.dd",
        "yyyy-MM",
        "yyyy",
    };

    private static readonly char[] PersonDelimiterChars = ['|', '/', ';', ',', '\n', '\r'];

    /// <inheritdoc />
    public string Name => "kodi-movie-nfo";

    /// <inheritdoc />
    public string DisplayName => "Kodi Movie NFO";

    /// <inheritdoc />
    public string Description => "Parses Kodi-style .nfo files for movie metadata.";

    /// <inheritdoc />
    public int Order => (int)MetadataAgentPriority.Sidecar;

    /// <inheritdoc />
    public IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; } = [LibraryType.Movies];

    /// <inheritdoc />
    public bool CanParse(FileSystemMetadata sidecarFile)
    {
        if (!sidecarFile.Exists || sidecarFile.IsDirectory)
        {
            return false;
        }

        return string.Equals(sidecarFile.Extension, ".nfo", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<SidecarParseResult?> ParseAsync(
        SidecarParseRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.LibraryType != LibraryType.Movies)
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var document = LoadDocument(request.SidecarFile.Path);
        var root = document.Root;
        if (
            root is null
            || !string.Equals(root.Name.LocalName, "movie", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var metadata = BuildMetadata(root, request.MediaFile);
        if (metadata is null)
        {
            return Task.FromResult<SidecarParseResult?>(null);
        }

        var people = BuildPeople(root);
        var hints = BuildHints(root);
        var genres = BuildGenres(root);
        var tags = BuildTags(root);
        var result = new SidecarParseResult(
            metadata,
            hints,
            this.Name,
            people,
            Genres: genres,
            Tags: tags
        );
        return Task.FromResult<SidecarParseResult?>(result);
    }

    private static XDocument LoadDocument(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        using var reader = XmlReader.Create(path, settings);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
    }

    private static Movie? BuildMetadata(XElement root, FileSystemMetadata mediaFile)
    {
        var title = GetString(root, "title") ?? GetFileNameWithoutExtension(mediaFile.Path);
        var sortTitle = GetString(root, "sorttitle") ?? title;
        var originalTitle = GetString(root, "originaltitle");
        var summary = GetString(root, "plot") ?? GetString(root, "outline");
        var tagline = GetString(root, "tagline");
        var (contentRating, countryCode) = ParseContentRating(
            GetString(root, "mpaa") ?? GetString(root, "certification")
        );

        var releaseDate = ParseDate(
            GetString(root, "premiered")
                ?? GetString(root, "releasedate")
                ?? GetString(root, "aired")
                ?? GetString(root, "date")
        );

        var year = ParseYear(GetString(root, "year")) ?? releaseDate?.Year;
        var durationSeconds = ParseDurationSeconds(GetString(root, "runtime"));

        var thumb = GetThumb(root);
        var art = GetFanArt(root);

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new Movie
        {
            Title = title,
            SortTitle = string.IsNullOrWhiteSpace(sortTitle) ? title : sortTitle,
            OriginalTitle = originalTitle,
            Summary = summary,
            Tagline = tagline,
            ContentRating = contentRating,
            ContentRatingCountryCode = countryCode,
            ReleaseDate = releaseDate,
            Year = year,
            Duration = durationSeconds,
            ThumbUri = thumb,
            ArtUri = art,
        };
    }

    private static List<PersonCredit>? BuildPeople(XElement root)
    {
        var credits = new List<(PersonCredit Credit, int? Order, int Sequence)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sequence = 0;

        foreach (
            var actor in root.Elements()
                .Where(e =>
                    string.Equals(e.Name.LocalName, "actor", StringComparison.OrdinalIgnoreCase)
                )
        )
        {
            var name = GetString(actor, "name");
            var role = GetString(actor, "role");
            var thumb = GetString(actor, "thumb");
            var order = ParseActorOrder(actor);
            var credit = CreatePersonCredit(
                seen,
                name,
                RelationType.PersonPerformsInVideo,
                role,
                thumb
            );

            if (credit is not null)
            {
                credits.Add((credit, order, sequence++));
            }
        }

        AddDelimitedPeople(
            GetString(root, "director"),
            RelationType.PersonContributesCrewToVideo,
            "Director",
            credits,
            seen,
            ref sequence
        );

        AddDelimitedPeople(
            GetString(root, "writer"),
            RelationType.PersonContributesCrewToVideo,
            "Writer",
            credits,
            seen,
            ref sequence
        );

        AddDelimitedPeople(
            GetString(root, "credits"),
            RelationType.PersonContributesCrewToVideo,
            "Writer",
            credits,
            seen,
            ref sequence
        );

        AddDelimitedPeople(
            GetString(root, "composer"),
            RelationType.PersonContributesMusicToVideo,
            "Composer",
            credits,
            seen,
            ref sequence
        );

        AddDelimitedPeople(
            GetString(root, "artist"),
            RelationType.PersonContributesMusicToVideo,
            "Artist",
            credits,
            seen,
            ref sequence
        );

        if (credits.Count == 0)
        {
            return null;
        }

        return credits
            .OrderBy(c => c.Order ?? int.MaxValue)
            .ThenBy(c => c.Sequence)
            .Select(c => c.Credit)
            .ToList();
    }

    private static List<string>? BuildGenres(XElement root)
    {
        var genres = root.Elements()
            .Where(e =>
                string.Equals(e.Name.LocalName, "genre", StringComparison.OrdinalIgnoreCase)
            )
            .Select(e => e.Value?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return genres.Count > 0 ? genres : null;
    }

    private static List<string>? BuildTags(XElement root)
    {
        var tags = root.Elements()
            .Where(e => string.Equals(e.Name.LocalName, "tag", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tags.Count > 0 ? tags : null;
    }

    private static void AddDelimitedPeople(
        string? value,
        RelationType relationType,
        string? text,
        List<(PersonCredit Credit, int? Order, int Sequence)> credits,
        HashSet<string> seen,
        ref int sequence
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var parts = value.Split(
            PersonDelimiterChars,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        foreach (var part in parts)
        {
            var credit = CreatePersonCredit(seen, part, relationType, text, thumb: null);
            if (credit is not null)
            {
                credits.Add((credit, null, sequence++));
            }
        }
    }

    private static PersonCredit? CreatePersonCredit(
        HashSet<string> seen,
        string? name,
        RelationType relationType,
        string? text,
        string? thumb
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim();
        var key = $"{relationType}:{normalizedName}:{text}".ToLowerInvariant();
        if (!seen.Add(key))
        {
            return null;
        }

        var person = new Person
        {
            Title = normalizedName,
            SortTitle = normalizedName,
            ThumbUri = string.IsNullOrWhiteSpace(thumb) ? null : thumb.Trim(),
        };

        return new PersonCredit(person, relationType, text);
    }

    private static int? ParseActorOrder(XElement actor)
    {
        var orderValue = GetString(actor, "order");
        if (
            int.TryParse(
                orderValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var order
            )
        )
        {
            return order;
        }

        return null;
    }

    private static Dictionary<string, object>? BuildHints(XElement root)
    {
        var externalIds = CollectExternalIds(root);
        if (externalIds.Count == 0)
        {
            return null;
        }

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "external_ids", externalIds },
        };
    }

    private static Dictionary<string, string> CollectExternalIds(XElement root)
    {
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Prefer uniqueid elements; fall back to legacy fields
        var uniqueIds = root.Elements()
            .Where(e =>
                string.Equals(e.Name.LocalName, "uniqueid", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
        if (uniqueIds.Count > 0)
        {
            // Prefer default="true" entries
            foreach (var element in uniqueIds.OrderByDescending(IsDefaultUniqueId))
            {
                var type = element.Attribute("type")?.Value?.Trim();
                var value = element.Value?.Trim();
                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (!ids.ContainsKey(type))
                {
                    ids[type] = value;
                }
            }
        }

        // Legacy fallbacks
        AddIfNotEmpty(ids, "imdb", GetString(root, "id") ?? GetString(root, "imdbid"));
        AddIfNotEmpty(ids, "tmdb", GetString(root, "tmdbid"));

        return ids;
    }

    private static bool IsDefaultUniqueId(XElement element)
    {
        var defaultAttr = element.Attribute("default")?.Value;
        return bool.TryParse(defaultAttr, out var isDefault) && isDefault;
    }

    private static string? GetThumb(XElement root)
    {
        // Prefer explicit <thumb> at movie level
        var thumb = root.Elements()
            .FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, "thumb", StringComparison.OrdinalIgnoreCase)
            );
        if (!string.IsNullOrWhiteSpace(thumb?.Value))
        {
            return thumb.Value.Trim();
        }

        // Fallback to first fanart thumb
        var fanArtThumb = root.Element("fanart")
            ?.Elements()
            .FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, "thumb", StringComparison.OrdinalIgnoreCase)
            );

        return string.IsNullOrWhiteSpace(fanArtThumb?.Value) ? null : fanArtThumb.Value.Trim();
    }

    private static string? GetFanArt(XElement root)
    {
        var fanart = root.Element("fanart");
        if (fanart is null)
        {
            return null;
        }

        var thumb = fanart
            .Elements()
            .FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, "thumb", StringComparison.OrdinalIgnoreCase)
            );
        if (!string.IsNullOrWhiteSpace(thumb?.Value))
        {
            return thumb.Value.Trim();
        }

        // Support <fanart> value directly
        return string.IsNullOrWhiteSpace(fanart.Value) ? null : fanart.Value.Trim();
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (
            DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOnly
            )
        )
        {
            return dateOnly;
        }

        if (
            DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dt
            )
        )
        {
            return DateOnly.FromDateTime(dt); // Normalize to date-only
        }

        foreach (var format in DateFormats)
        {
            if (
                DateOnly.TryParseExact(
                    value,
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var exact
                )
            )
            {
                return exact;
            }
        }

        return null;
    }

    private static int? ParseYear(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            return year;
        }

        return null;
    }

    private static int? ParseDurationSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Kodi runtime is minutes; tolerate trailing labels
        var trimmed = value.Trim();
        var numberPart = new string(trimmed.TakeWhile(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(numberPart))
        {
            return null;
        }

        if (
            !int.TryParse(
                numberPart,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var minutes
            )
        )
        {
            return null;
        }

        return minutes > 0 ? minutes * 60 : null;
    }

    private static string? GetString(XElement parent, string name)
    {
        var element = parent.Element(name);
        var value = element?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GetFileNameWithoutExtension(string path)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
    }

    private static void AddIfNotEmpty(Dictionary<string, string> target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !target.ContainsKey(key))
        {
            target[key] = value.Trim();
        }
    }

    /// <summary>
    /// Parses a content rating string and extracts the rating identifier and optional country code.
    /// Supports formats: "US:PG-13", "UK-15", "[US] PG-13", "PG-13 (US)", "PG-13 / UK:12A", etc.
    /// For multi-country ratings, only the first valid pair is returned.
    /// </summary>
    /// <param name="rawRating">The raw content rating string from the NFO file.</param>
    /// <returns>A tuple containing the normalized rating identifier and optional ISO 3166-1 alpha-2 country code.</returns>
    private static (string? Rating, string? CountryCode) ParseContentRating(string? rawRating)
    {
        if (string.IsNullOrWhiteSpace(rawRating))
        {
            return (null, null);
        }

        var normalized = rawRating.Trim();

        // Split on common multi-country delimiters (e.g., "US:PG-13 / UK:12A")
        var parts = normalized.Split(
            ['/', '|'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var firstPart = parts.Length > 0 ? parts[0] : normalized;

        // Try to extract country code from various formats
        // Pattern 1: Prefix formats like "US:PG-13", "US - PG-13", "[US] PG-13"
        // Pattern 2: Suffix format like "PG-13 (US)"
        // Use colon or space as separators, but NOT hyphen alone to avoid matching "PG-13" as "PG" + "13"
        var match = Regex.Match(
            firstPart,
            @"^(?:\[)?([A-Z]{2})(?:\])?(?:[\s]*:[\s]*|\s+)(.+)$|^(.+?)[\s]*\(([A-Z]{2})\)$",
            RegexOptions.IgnoreCase
        );

        if (match.Success)
        {
            // Prefix format: "US:PG-13" or "US PG-13" -> Groups[1] = country, Groups[2] = rating
            if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                var country = match.Groups[1].Value.Trim().ToUpperInvariant();
                var rating = match.Groups[2].Value.Trim();
                return (rating, country);
            }

            // Suffix format: "PG-13 (US)" -> Groups[3] = rating, Groups[4] = country
            if (!string.IsNullOrWhiteSpace(match.Groups[3].Value))
            {
                var rating = match.Groups[3].Value.Trim();
                var country = match.Groups[4].Value.Trim().ToUpperInvariant();
                return (rating, country);
            }
        }

        // No country code detected; return rating as-is
        return (firstPart, null);
    }
}
