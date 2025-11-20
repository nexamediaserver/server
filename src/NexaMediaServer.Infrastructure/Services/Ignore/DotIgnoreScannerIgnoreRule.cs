// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Ignore;

/// <summary>
/// Implements a simplified .ignore rule similar to Jellyfin's <c>DotIgnoreIgnoreRule</c>.
/// If a directory contains an empty <c>.ignore</c> file we skip the entire subtree.
/// Non-empty files are currently ignored (future: parse patterns). This keeps implementation light without extra dependencies.
/// </summary>
public sealed class DotIgnoreScannerIgnoreRule : IScannerIgnoreRule
{
    /// <summary>
    /// Determines whether a directory should be ignored based on presence of an empty .ignore file.
    /// </summary>
    /// <param name="directoryPath">Directory path.</param>
    /// <param name="parentDirectory">Parent directory path.</param>
    /// <returns>True to ignore.</returns>
    public bool ShouldIgnoreDirectory(string directoryPath, string? parentDirectory)
    {
        try
        {
            var ignoreFile = Path.Combine(directoryPath, ".ignore");
            if (!File.Exists(ignoreFile))
            {
                return false;
            }

            // Fast path: empty file -> ignore subtree
            var info = new FileInfo(ignoreFile);
            if (info.Length == 0)
            {
                return true;
            }

            // Future enhancement: parse patterns in fileContent to ignore selectively
            return false; // Non-empty currently treated as pattern list placeholder
        }
        catch
        {
            // If we can't access treat as non-ignored (fail-safe) to avoid hiding data unexpectedly
            return false;
        }
    }

    /// <summary>
    /// File-level ignore always returns false until pattern parsing implemented.
    /// </summary>
    /// <param name="filePath">File path.</param>
    /// <param name="parentDirectory">Parent directory.</param>
    /// <returns>False.</returns>
    public bool ShouldIgnoreFile(string filePath, string? parentDirectory)
    {
        // File-level pattern support deferred (handled by Core rule for now)
        return false;
    }
}
