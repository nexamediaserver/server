// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Runs item resolvers to classify filesystem entries into metadata items.
/// </summary>
public sealed partial class ResolveItemsStage : IScanPipelineStage<ScanWorkItem, ScanWorkItem>
{
    /// <summary>
    /// Interval between cache pruning operations to limit memory growth.
    /// </summary>
    private const int PruneIntervalItems = 5000;

    private readonly IPartsRegistry partsRegistry;
    private readonly ILogger<ResolveItemsStage> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveItemsStage"/> class.
    /// </summary>
    /// <param name="partsRegistry">Registry containing discovered resolvers.</param>
    /// <param name="logger">Typed logger.</param>
    public ResolveItemsStage(IPartsRegistry partsRegistry, ILogger<ResolveItemsStage> logger)
    {
        this.partsRegistry = partsRegistry;
        this.logger = logger;
    }

    /// <inheritdoc />
    public string Name => "resolve_items";

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<ScanWorkItem> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var resolvers = this.partsRegistry.ItemResolvers;

        // Use a bounded cache that only keeps ancestor paths needed for resolution.
        // This prevents unbounded memory growth during large library scans.
        var resolvedByPath =
            new Dictionary<string, (MetadataBaseItem Metadata, FileSystemMetadata File)>(
                StringComparer.OrdinalIgnoreCase
            );

        // Track the set of paths that are still valid as potential parents
        var activeParentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var itemCount = 0;

        await foreach (var item in input.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parentPath = Path.GetDirectoryName(item.File.Path);
            var resolvedParent = TryGetResolvedParent(parentPath, resolvedByPath);
            var ancestors = BuildAncestors(parentPath, resolvedByPath);

            var args = new ItemResolveArgs(
                item.File,
                context.LibrarySection.Type,
                item.Location.Id,
                context.LibrarySection.Id,
                item.Children,
                item.IsRoot,
                ancestors,
                resolvedParent,
                item.Siblings
            );

            MetadataBaseItem? resolved = null;
            string? resolverName = null;
            var sw = Stopwatch.StartNew();
            foreach (var resolver in resolvers)
            {
                resolved = resolver.Resolve(args);
                if (resolved != null)
                {
                    resolverName = resolver.Name;
                    break;
                }
            }

            sw.Stop();

            if (resolved == null)
            {
                LogUnclaimed(this.logger, item.File.Path);
                continue;
            }

            LogItemResolverFinished(
                this.logger,
                resolverName!,
                item.File.Path,
                sw.ElapsedMilliseconds
            );

            // Only cache directories since files cannot be parents
            if (item.File.IsDirectory)
            {
                resolvedByPath[item.File.Path] = (resolved, item.File);
                activeParentPaths.Add(item.File.Path);

                // Also mark the full ancestor chain as active
                var ancestor = Path.GetDirectoryName(item.File.Path);
                while (!string.IsNullOrEmpty(ancestor))
                {
                    activeParentPaths.Add(ancestor);
                    ancestor = Path.GetDirectoryName(ancestor);
                }
            }

            itemCount++;

            // Periodically prune entries that are no longer needed as parents
            // to prevent unbounded memory growth in large libraries
            if (itemCount % PruneIntervalItems == 0)
            {
                PruneStaleEntries(resolvedByPath, activeParentPaths);
                activeParentPaths.Clear();
            }

            yield return item with
            {
                ResolvedMetadata = resolved,
            };
        }
    }

    private static void PruneStaleEntries(
        Dictionary<string, (MetadataBaseItem Metadata, FileSystemMetadata File)> cache,
        HashSet<string> activeParentPaths
    )
    {
        // Remove entries that are not in the active parent path set
        // These are directories that have been fully processed and won't be needed again
        var keysToRemove = cache.Keys.Where(k => !activeParentPaths.Contains(k)).ToList();
        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
        }
    }

    private static MetadataBaseItem? TryGetResolvedParent(
        string? parentPath,
        Dictionary<string, (MetadataBaseItem Metadata, FileSystemMetadata File)> resolved
    )
    {
        if (string.IsNullOrEmpty(parentPath))
        {
            return null;
        }

        return resolved.TryGetValue(parentPath, out var entry) ? entry.Metadata : null;
    }

    private static List<AncestorInfo>? BuildAncestors(
        string? parentPath,
        Dictionary<string, (MetadataBaseItem Metadata, FileSystemMetadata File)> resolved
    )
    {
        if (string.IsNullOrEmpty(parentPath))
        {
            return null;
        }

        var ancestors = new List<AncestorInfo>();
        var current = parentPath;

        while (!string.IsNullOrEmpty(current))
        {
            if (resolved.TryGetValue(current, out var entry))
            {
                ancestors.Insert(0, new AncestorInfo(current, entry.File, entry.Metadata));
            }

            current = Path.GetDirectoryName(current);
        }

        return ancestors.Count == 0 ? null : ancestors;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping unclaimed file {Path}")]
    private static partial void LogUnclaimed(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Item resolver {ResolverName} finished for {Path} in {ElapsedMs}ms"
    )]
    private static partial void LogItemResolverFinished(
        ILogger logger,
        string resolverName,
        string path,
        long elapsedMs
    );
}
