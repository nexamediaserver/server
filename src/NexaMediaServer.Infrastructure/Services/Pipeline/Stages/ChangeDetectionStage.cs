// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Marks work items as unchanged when the file already exists in the library and can be skipped.
/// </summary>
public sealed class ChangeDetectionStage : IScanPipelineStage<ScanWorkItem, ScanWorkItem>
{
    private readonly IMediaPartRepository mediaPartRepository;
    private readonly Dictionary<int, HashSet<string>> cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeDetectionStage"/> class.
    /// </summary>
    /// <param name="mediaPartRepository">Repository for existing media parts.</param>
    public ChangeDetectionStage(IMediaPartRepository mediaPartRepository)
    {
        this.mediaPartRepository = mediaPartRepository;
    }

    /// <inheritdoc />
    public string Name => "change_detection";

    /// <inheritdoc />
    public async IAsyncEnumerable<ScanWorkItem> ExecuteAsync(
        IAsyncEnumerable<ScanWorkItem> input,
        IPipelineContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var existingPaths = await this.GetExistingPathsAsync(
            context.LibrarySection.Id,
            cancellationToken
        );

        await foreach (var item in input.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.File.IsDirectory && existingPaths.Contains(item.File.Path))
            {
                yield return item with
                {
                    IsUnchanged = true,
                };
                continue;
            }

            yield return item;
        }
    }

    private async Task<HashSet<string>> GetExistingPathsAsync(
        int libraryId,
        CancellationToken cancellationToken
    )
    {
        if (this.cache.TryGetValue(libraryId, out var cached))
        {
            return cached;
        }

        var paths = await this.mediaPartRepository.GetFilePathsByLibraryIdAsync(
            libraryId,
            cancellationToken
        );
        this.cache[libraryId] = paths;
        return paths;
    }
}
