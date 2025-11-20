// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Hosting;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides access to the application's directory paths for data, configuration, logs, cache, and other resources.
/// </summary>
public interface IApplicationPaths : IHostedService
{
    /// <summary>
    /// Gets the base data directory for the application.
    /// </summary>
    string DataDirectory { get; }

    /// <summary>
    /// Gets the configuration files directory.
    /// </summary>
    string ConfigDirectory { get; }

    /// <summary>
    /// Gets the log files directory.
    /// </summary>
    string LogDirectory { get; }

    /// <summary>
    /// Gets the cache directory (thumbnails, transcoded files, etc.)
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Gets the media files directory (original user-provided media content).
    /// </summary>
    string MediaDirectory { get; }

    /// <summary>
    /// Gets the temporary files directory.
    /// </summary>
    string TempDirectory { get; }

    /// <summary>
    /// Gets the database directory.
    /// </summary>
    string DatabaseDirectory { get; }

    /// <summary>
    /// Gets the search index directory.
    /// </summary>
    string IndexDirectory { get; }

    /// <summary>
    /// Gets the backup directory.
    /// </summary>
    string BackupDirectory { get; }

    /// <summary>
    /// Ensures that the specified directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path to ensure exists.</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Gets a data path by combining the data directory with the specified path segments.
    /// </summary>
    /// <param name="paths">The path segments to combine with the data directory.</param>
    /// <returns>The combined data path.</returns>
    string GetDataPath(params string[] paths);
}
