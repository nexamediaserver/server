// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Ignore;

/// <summary>
/// Represents a rule that can decide whether a path should be ignored during scanning.
/// </summary>
public interface IScannerIgnoreRule
{
    /// <summary>
    /// Gets a unique identifier for this ignore rule to use for logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns true if the directory (and its subtree) should be ignored.
    /// </summary>
    /// <param name="directoryPath">Absolute path to the directory.</param>
    /// <param name="parentDirectory">Optional parent directory path.</param>
    /// <returns>True when the directory should be skipped, otherwise false.</returns>
    bool ShouldIgnoreDirectory(string directoryPath, string? parentDirectory);

    /// <summary>
    /// Returns true if the file should be ignored.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="parentDirectory">Directory containing the file.</param>
    /// <returns>True when the file should be skipped, otherwise false.</returns>
    bool ShouldIgnoreFile(string filePath, string? parentDirectory);
}
