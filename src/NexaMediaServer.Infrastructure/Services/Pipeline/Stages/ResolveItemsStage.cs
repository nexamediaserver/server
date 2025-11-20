// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Diagnostics;
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

        await foreach (var item in input.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var args = new ItemResolveArgs(
                item.File,
                context.LibrarySection.Type,
                item.Location.Id,
                context.LibrarySection.Id,
                item.Children,
                item.IsRoot,
                item.Ancestors,
                item.ResolvedParent,
                Siblings: null
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

            yield return item with
            {
                ResolvedMetadata = resolved,
            };
        }
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
