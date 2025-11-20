// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text;

namespace NexaMediaServer.Common;

/// <summary>
/// Utilities for generating canonical sort names from display titles.
/// </summary>
public static class SortName
{
    // Note: We remove at most ONE leading article.

    /// <summary>
    /// Generates a canonical sort name from a display title using the current UI culture's language for article handling.
    /// Prefer the overload that accepts a language when the content language is known.
    /// </summary>
    /// <param name="title">The display title.</param>
    /// <returns>A normalized sort name.</returns>
    public static string Generate(string? title) =>
        Generate(title, language: CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    /// <summary>
    /// Generates a canonical sort name from a display title, removing a single leading article appropriate for the given language.
    /// The <paramref name="language"/> should be a BCP-47 language tag or ISO-639-1 code (e.g., "en", "en-US", "fr", "de").
    /// If the language is null or unknown, no article removal is performed.
    /// </summary>
    /// <param name="title">The display title.</param>
    /// <param name="language">The language code (e.g., "en", "fr-CA").</param>
    /// <returns>A normalized sort name.</returns>
    public static string Generate(string? title, string? language)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        // Normalize Unicode (NFC) and trim outer whitespace.
        var s = title.Normalize(NormalizationForm.FormC).Trim();

        // Drop leading punctuation/symbols/quotes/brackets.
        s = TrimLeadingNonAlnum(s);

        // Remove one leading initial article if present, based on language when provided.
        s = RemoveLeadingArticle(s, language);

        // After article removal, we may expose a trailing bracket/quote at the start
        // (e.g., "(The) Foo" -> ") Foo"). Trim leading punctuation/symbols again.
        s = TrimLeadingNonAlnum(s);

        // Final tidy: single trim for any extra leading space left by removal.
        s = s.TrimStart();

        return s;
    }

    private static string TrimLeadingNonAlnum(string s)
    {
        var i = 0;
        while (i < s.Length)
        {
            var ch = s[i];
            // Keep letters and digits; skip whitespace, punctuation, and symbols at the start.
            if (char.IsLetterOrDigit(ch))
            {
                break;
            }

            if (!char.IsWhiteSpace(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
            {
                break;
            }

            i++;
        }

        return i > 0 && i <= s.Length ? s[i..] : s;
    }

    private static string RemoveLeadingArticle(string s, string? language)
    {
        if (s.Length == 0)
        {
            return s;
        }

        // Resolve language to a primary subtag (e.g., "en-US" -> "en").
        var lang = NormalizeLanguage(language);

        // Language-specific sets; if unknown, do not remove any article.
        var sets = GetArticleSets(lang);
        if (sets is null)
        {
            return s;
        }

        var (wordArticles, elidedArticles) = sets.Value;

        // Check explicit elided forms first (e.g., "l'", "d’").
        var elidedMatch = elidedArticles.FirstOrDefault(elided =>
            s.StartsWith(elided, StringComparison.InvariantCultureIgnoreCase)
        );
        if (elidedMatch is not null)
        {
            return s[elidedMatch.Length..];
        }

        // Then check word articles followed by a boundary.
        // We only strip exactly one article, and consider boundary characters:
        //  - whitespace (e.g., "The Office")
        //  - closing punctuation/symbols immediately after the article (e.g., "The) Leftovers", "The] Mandalorian", "The" Wire")
        string? articleMatch = null;
        for (var k = 0; k < wordArticles.Length; k++)
        {
            var article = wordArticles[k];
            if (
                s.Length > article.Length
                && s.StartsWith(article, StringComparison.InvariantCultureIgnoreCase)
                && (articleMatch is null || article.Length > articleMatch.Length)
            )
            {
                articleMatch = article;
            }
        }

        if (articleMatch is not null)
        {
            var j = articleMatch.Length;

            // If immediately followed by punctuation/symbol(s), skip them.
            while (j < s.Length && (char.IsPunctuation(s[j]) || char.IsSymbol(s[j])))
            {
                j++;
            }

            // If followed by a single whitespace, skip it as well.
            if (j < s.Length && char.IsWhiteSpace(s[j]))
            {
                j++;
            }

            // Only remove if we actually had a valid boundary after the article
            // (i.e., not mid-word like "Theremin"). Ensure original next char wasn't a letter/digit.
            var nextCharIndex = articleMatch.Length;
            if (nextCharIndex >= s.Length || !char.IsLetterOrDigit(s[nextCharIndex]))
            {
                return s[j..];
            }
        }

        return s;
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var idx = language.IndexOfAny(['-', '_']);
        return (idx > 0 ? language[..idx] : language).Trim().ToLowerInvariant();
    }

    private static (string[] Words, string[] Elided)? GetArticleSets(string? lang)
    {
        if (string.IsNullOrEmpty(lang))
        {
            return null;
        }

        // Minimal but representative per-language sets. Expand as needed.
        return lang switch
        {
            // English
            "en" => (new[] { "a", "an", "the" }, Array.Empty<string>()),

            // French
            "fr" => (
                new[] { "le", "la", "les", "un", "une", "des" },
                new[]
                {
                    "l'",
                    "l’",
                    "d'",
                    "d’",
                    "t'",
                    "t’",
                    "s'",
                    "s’",
                    "j'",
                    "j’",
                    "n'",
                    "n’",
                    "qu'",
                    "qu’",
                }
            ),

            // German
            "de" => (new[] { "der", "die", "das", "ein", "eine" }, Array.Empty<string>()),

            // Spanish
            "es" => (new[] { "el", "la", "los", "las", "un", "una" }, Array.Empty<string>()),

            // Italian
            "it" => (
                new[] { "il", "lo", "la", "i", "gli", "le", "uno", "una" },
                new[] { "l'", "l’", "d'", "d’" }
            ),

            // Portuguese
            "pt" => (new[] { "o", "a", "os", "as", "um", "uma" }, Array.Empty<string>()),

            // Dutch
            "nl" => (new[] { "de", "het", "een" }, Array.Empty<string>()),

            // Swedish
            "sv" => (new[] { "en", "ett", "den", "det", "de" }, Array.Empty<string>()),

            // Norwegian (Bokmål/Nynorsk simplified)
            "no" or "nb" or "nn" => (
                new[] { "en", "ei", "et", "den", "det", "de" },
                Array.Empty<string>()
            ),

            // Danish
            "da" => (new[] { "en", "et", "den", "det", "de" }, Array.Empty<string>()),

            _ => null,
        };
    }
}
