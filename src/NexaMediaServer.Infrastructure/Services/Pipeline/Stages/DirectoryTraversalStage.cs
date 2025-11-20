// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// Number of items to process before saving a checkpoint.
    /// </summary>
    private const int CheckpointItemThreshold = 500;

    /// <summary>
    /// Maximum time between checkpoints in seconds.
    /// </summary>
    private const int CheckpointTimeThresholdSeconds = 30;

    /// <summary>
    /// Number of paths to batch before recording seen paths.
    /// </summary>
    private const int SeenPathBatchSize = 200;

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
        var state = new TraversalState(context);

        await foreach (var location in input.WithCancellation(cancellationToken))
        {
            await foreach (
                var workItem in this.ProcessLocationAsync(location, state, cancellationToken)
                    .WithCancellation(cancellationToken)
            )
            {
                yield return workItem;
            }
        }

        await state.FlushAsync(this.Name, cancellationToken).ConfigureAwait(false);
    }

    private static ScanWorkItem CreateWorkItem(
        SectionLocation location,
        FileSystemMetadata file,
        IReadOnlyList<FileSystemMetadata> siblings
    )
    {
        var children = file.IsDirectory ? SafeEnumerateChildren(file.Path) : null;

        return new ScanWorkItem
        {
            Location = location,
            File = file,
            Children = children,
            Siblings = siblings,
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

    private async IAsyncEnumerable<ScanWorkItem> ProcessLocationAsync(
        SectionLocation location,
        TraversalState state,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
#pragma warning disable S3267 // We need batch context to pass siblings for sidecar lookup optimization
        await foreach (
            var batch in this.fileScanner.ScanDirectoryStreamingAsync(
                location.RootPath,
                cancellationToken
            )
        )
#pragma warning restore S3267
        {
            var siblings = batch.Files;

            foreach (var file in batch.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (state.ShouldSkipForResume(file.Path))
                {
                    continue;
                }

                await state
                    .RecordAndCheckpointAsync(file.Path, this.Name, cancellationToken)
                    .ConfigureAwait(false);

                yield return CreateWorkItem(location, file, siblings);
            }
        }
    }

    /// <summary>
    /// Encapsulates traversal state including resume logic, seen paths buffering, and checkpointing.
    /// </summary>
    private sealed class TraversalState
    {
        private readonly IPipelineContext context;
        private readonly List<string> seenPathBuffer = new(SeenPathBatchSize);
        private readonly Stopwatch checkpointStopwatch = Stopwatch.StartNew();
        private readonly string? resumeFromPath;

        private bool isResuming;
        private int itemsSinceCheckpoint;
        private string? lastProcessedPath;

        public TraversalState(IPipelineContext context)
        {
            this.context = context;
            var checkpoint = context.Checkpoint;
            this.isResuming =
                checkpoint?.Stage == "directory_traversal"
                && !string.IsNullOrEmpty(checkpoint.Cursor);
            this.resumeFromPath = this.isResuming ? checkpoint!.Cursor : null;
        }

        /// <summary>
        /// Determines if the given path should be skipped due to resume logic.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the path should be skipped.</returns>
        public bool ShouldSkipForResume(string path)
        {
            if (!this.isResuming)
            {
                return false;
            }

            if (string.Equals(path, this.resumeFromPath, StringComparison.OrdinalIgnoreCase))
            {
                // Found the resume point, stop skipping after this
                this.isResuming = false;
            }

            return true;
        }

        /// <summary>
        /// Records a seen path and saves a checkpoint if thresholds are met.
        /// </summary>
        /// <param name="path">The file path that was processed.</param>
        /// <param name="stageName">The current stage name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        public async ValueTask RecordAndCheckpointAsync(
            string path,
            string stageName,
            CancellationToken cancellationToken
        )
        {
            this.lastProcessedPath = path;

            // Buffer seen paths for batch recording
            this.seenPathBuffer.Add(path);
            if (this.seenPathBuffer.Count >= SeenPathBatchSize)
            {
                await this
                    .context.RecordSeenPathsAsync(this.seenPathBuffer, cancellationToken)
                    .ConfigureAwait(false);
                this.seenPathBuffer.Clear();
            }

            this.itemsSinceCheckpoint++;

            // Save checkpoint periodically
            var shouldCheckpoint =
                this.itemsSinceCheckpoint >= CheckpointItemThreshold
                || this.checkpointStopwatch.Elapsed.TotalSeconds >= CheckpointTimeThresholdSeconds;

            if (shouldCheckpoint)
            {
                await this
                    .context.SaveCheckpointAsync(stageName, path, cancellationToken)
                    .ConfigureAwait(false);
                this.itemsSinceCheckpoint = 0;
                this.checkpointStopwatch.Restart();
            }
        }

        /// <summary>
        /// Flushes any remaining buffered paths and saves final checkpoint.
        /// </summary>
        /// <param name="stageName">The current stage name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        public async ValueTask FlushAsync(string stageName, CancellationToken cancellationToken)
        {
            if (this.seenPathBuffer.Count > 0)
            {
                await this
                    .context.RecordSeenPathsAsync(this.seenPathBuffer, cancellationToken)
                    .ConfigureAwait(false);
                this.seenPathBuffer.Clear();
            }

            if (this.lastProcessedPath != null)
            {
                await this
                    .context.SaveCheckpointAsync(
                        stageName,
                        this.lastProcessedPath,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
        }
    }
}
