// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Parts;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Service for scanning directories and identifying media files.
/// </summary>
public partial class FileScanner : IFileScanner
{
    private readonly ILogger<FileScanner> logger;
    private readonly IScannerIgnoreRule[] ignoreRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileScanner"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="partsRegistry">The parts registry providing discovered ignore rules.</param>
    public FileScanner(ILogger<FileScanner> logger, IPartsRegistry partsRegistry)
    {
        this.logger = logger;
        this.ignoreRules = partsRegistry.ScannerIgnoreRules.ToArray();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ScannedDirectoryBatch> ScanDirectoryStreamingAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // Ensure truly asynchronous execution (and satisfy analyzers) even if no awaited I/O occurs in early paths
        await Task.Yield();

        if (!Directory.Exists(path))
        {
            this.LogDirectoryNotFound(path);
            yield break;
        }

        // No extension filtering here; ignore rules only. Resolution happens downstream.
        var stack = new Stack<string>();
        stack.Push(path);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = stack.Pop();
            var parentDirectory = Path.GetDirectoryName(current);

            // Check directory-level ignore rules
            var matchingDirRule = this.ignoreRules.FirstOrDefault(r =>
                r.ShouldIgnoreDirectory(current, parentDirectory)
            );
            if (matchingDirRule != null)
            {
                this.LogIgnoreRuleMatchedDirectory(matchingDirRule.Name, current);
                continue; // Skip subtree entirely
            }

            var files = new List<Resolvers.FileSystemMetadata>();

            // Enumerate files in current directory
            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var matchingFileRule = this.ignoreRules.FirstOrDefault(r =>
                        r.ShouldIgnoreFile(file, current)
                    );
                    if (matchingFileRule != null)
                    {
                        this.LogIgnoreRuleMatchedFile(matchingFileRule.Name, file);
                        continue;
                    }

                    try
                    {
                        files.Add(Resolvers.FileSystemMetadata.FromPath(file));
                    }
                    catch (Exception ex)
                    {
                        this.LogFileAccessError(file, ex);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                this.LogDirectoryAccessDenied(current, ex);
            }
            catch (IOException ex)
            {
                this.LogDirectoryAccessDenied(current, ex);
            }

            // Enumerate subdirectories and add them to the batch as well
            // This allows directory-based resolvers (e.g., MusicAlbumResolver, MovieResolver) to claim directories
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(current))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        files.Add(Resolvers.FileSystemMetadata.FromPath(dir));
                    }
                    catch (Exception ex)
                    {
                        this.LogFileAccessError(dir, ex);
                    }

                    // We defer ignore evaluation for subdirectories until we pop them to allow parent-based rules.
                    stack.Push(dir);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                this.LogDirectoryAccessDenied(current, ex);
            }
            catch (IOException ex)
            {
                this.LogDirectoryAccessDenied(current, ex);
            }

            // Yield a batch for every directory, even if it contains zero files, so callers
            // can build a full directory graph (used for watchers, incremental scans, etc.).
            yield return new ScannedDirectoryBatch(current, files);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Directory does not exist: {Path}")]
    private partial void LogDirectoryNotFound(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied to directory: {Path}")]
    private partial void LogDirectoryAccessDenied(string path, Exception? exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error accessing file: {Path}")]
    private partial void LogFileAccessError(string path, Exception? exception);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Ignore rule {RuleName} matched directory {Path}"
    )]
    private partial void LogIgnoreRuleMatchedDirectory(string ruleName, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignore rule {RuleName} matched file {Path}")]
    private partial void LogIgnoreRuleMatchedFile(string ruleName, string path);

    private async Task ScanDirectoryRecursiveAsync(
        string path,
        List<Resolvers.FileSystemMetadata> files,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Get files in current directory
        foreach (var file in Directory.GetFiles(path))
        {
            try
            {
                files.Add(Resolvers.FileSystemMetadata.FromPath(file));
            }
            catch (Exception ex)
            {
                this.LogFileAccessError(file, ex);
            }
        }

        // Recursively scan subdirectories
        foreach (var directory in Directory.GetDirectories(path))
        {
            try
            {
                await this.ScanDirectoryRecursiveAsync(directory, files, cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                this.LogDirectoryAccessDenied(directory, ex);
            }
        }
    }
}
