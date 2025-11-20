// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;

namespace NexaMediaServer.Infrastructure.Services.Ignore;

/// <summary>
/// Core ignore rule providing built-in patterns analogous to Jellyfin's <c>CoreResolutionIgnoreRule</c> and <c>IgnorePatterns</c>.
/// This focuses on common noise files and special directories. Future expansion can adopt globbing if needed.
/// </summary>
public sealed class CoreScannerIgnoreRule : IScannerIgnoreRule
{
    // Directory names to ignore entirely (case-insensitive).
    private static readonly HashSet<string> IgnoredDirectoryNames = new(
        new[]
        {
            "extrafanart",
            "extrathumbs",
            ".actors",
            "lost+found",
            "#recycle",
            ".@__thumb",
            "@eaDir",
            "subs",
        },
        StringComparer.OrdinalIgnoreCase
    );

    // File name (exact) matches to ignore.
    private static readonly HashSet<string> IgnoredFileNames = new(
        new[] { "thumbs.db", ".ds_store" },
        StringComparer.OrdinalIgnoreCase
    );

    // Regex patterns similar to Jellyfin sample/trickplay and small artwork filtering.
    private static readonly Regex SampleVideoRegex = new(
        @"\bsample\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private static readonly Regex TrickplayRegex = new(
        @"\.trickplay(\.\w+)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private static readonly Regex HiddenFileRegex = new(
        @"^\.[^.].*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private static readonly Regex SmallImageRegex = new(
        @"(?i)(small|poster|albumart)\.(jpg|jpeg|png|webp)$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Determines whether the directory should be skipped entirely.
    /// </summary>
    /// <param name="directoryPath">Full directory path.</param>
    /// <param name="parentDirectory">Parent directory (null if root).</param>
    /// <returns>True to ignore the directory.</returns>
    public bool ShouldIgnoreDirectory(string directoryPath, string? parentDirectory)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return false;
        }

        var name = Path.GetFileName(
            directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return IgnoredDirectoryNames.Contains(name);
    }

    /// <summary>
    /// Determines whether the file should be skipped.
    /// </summary>
    /// <param name="filePath">Full file path.</param>
    /// <param name="parentDirectory">Containing directory path.</param>
    /// <returns>True to ignore the file.</returns>
    public bool ShouldIgnoreFile(string filePath, string? parentDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var fileName = Path.GetFileName(filePath);

        if (IgnoredFileNames.Contains(fileName))
        {
            return true;
        }

        // Hidden files (dot-prefixed) but allow "." and ".." semantics implicitly excluded by Path.GetFileName
        if (HiddenFileRegex.IsMatch(fileName))
        {
            return true;
        }

        // Sample / preview / trickplay detection
        if (SampleVideoRegex.IsMatch(fileName) || TrickplayRegex.IsMatch(fileName))
        {
            return true;
        }

        // Ignore small artwork assets that are likely auxiliary for other servers
        if (SmallImageRegex.IsMatch(fileName))
        {
            return true;
        }

        return false;
    }
}
