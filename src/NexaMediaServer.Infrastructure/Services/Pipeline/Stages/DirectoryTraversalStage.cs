// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Services;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Traverses library locations and streams filesystem entries as pipeline work items.
/// </summary>
public sealed class DirectoryTraversalStage : IScanPipelineStage<SectionLocation, ScanWorkItem>
{
    private readonly IFileScanner fileScanner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryTraversalStage"/> class.
    /// </summary>
    /// <param name="fileScanner">Streaming file scanner.</param>
    public DirectoryTraversalStage(IFileScanner fileScanner)
    {
        this.fileScanner = fileScanner;
    }

    /// <inheritdoc />
    public string Name => "directory_traversal";

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<SectionLocation> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var location in input.WithCancellation(cancellationToken))
        {
            await foreach (
                var batch in this.fileScanner.ScanDirectoryStreamingAsync(
                    location.RootPath,
                    cancellationToken
                )
            )
            {
                foreach (var file in batch.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var children = file.IsDirectory ? SafeEnumerateChildren(file.Path) : null;
                    yield return new ScanWorkItem
                    {
                        Location = location,
                        File = file,
                        Children = children,
                        Ancestors = null,
                        ResolvedParent = null,
                        Hints = null,
                        ResolvedMetadata = null,
                        Sidecar = null,
                        Embedded = null,
                        IsRoot = IsRootPath(location.RootPath, file.Path),
                        IsUnchanged = false,
                    };
                }
            }
        }
    }

    private static bool IsRootPath(string rootPath, string path)
    {
        return string.Equals(
            Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static List<FileSystemMetadata>? SafeEnumerateChildren(string directoryPath)
    {
        try
        {
            return System
                .IO.Directory.EnumerateFileSystemEntries(directoryPath)
                .Select(FileSystemMetadata.FromPath)
                .ToList();
        }
        catch
        {
            return null;
        }
    }
}
