// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services.Pipeline;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Pipeline;

/// <summary>
/// Concrete pipeline context used during scans.
/// </summary>
public sealed class ScanPipelineContext : IPipelineContext
{
    private readonly Func<ScanPipelineProgress, CancellationToken, ValueTask>? progressReporter;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanPipelineContext"/> class.
    /// </summary>
    /// <param name="librarySection">The library section being scanned.</param>
    /// <param name="scan">The scan record.</param>
    /// <param name="dbContextFactory">Factory for creating database contexts.</param>
    /// <param name="progressReporter">Optional callback for progress reporting.</param>
    /// <param name="checkpoint">Optional checkpoint data for resuming a scan.</param>
    public ScanPipelineContext(
        LibrarySection librarySection,
        LibraryScan scan,
        IDbContextFactory<MediaServerContext> dbContextFactory,
        Func<ScanPipelineProgress, CancellationToken, ValueTask>? progressReporter = null,
        ScanCheckpoint? checkpoint = null
    )
    {
        this.LibrarySection =
            librarySection ?? throw new ArgumentNullException(nameof(librarySection));
        this.Scan = scan ?? throw new ArgumentNullException(nameof(scan));
        this.dbContextFactory =
            dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        this.progressReporter = progressReporter;
        this.Checkpoint = checkpoint;
    }

    /// <inheritdoc />
    public LibrarySection LibrarySection { get; }

    /// <inheritdoc />
    public LibraryScan Scan { get; }

    /// <inheritdoc />
    public ScanCheckpoint? Checkpoint { get; private set; }

    /// <inheritdoc />
    public ValueTask ReportProgressAsync(
        ScanPipelineProgress progress,
        CancellationToken cancellationToken
    )
    {
        if (this.progressReporter is null)
        {
            return ValueTask.CompletedTask;
        }

        return this.progressReporter(progress, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveCheckpointAsync(
        string stage,
        string? cursor,
        CancellationToken cancellationToken
    )
    {
        await using var context = await this
            .dbContextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var newVersion = this.Scan.CheckpointVersion + 1;

        await context
            .LibraryScans.Where(s => s.Id == this.Scan.Id)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.CurrentStage, stage)
                        .SetProperty(x => x.ResumeCursor, cursor)
                        .SetProperty(x => x.LastCheckpointAt, DateTime.UtcNow)
                        .SetProperty(x => x.CheckpointVersion, newVersion),
                cancellationToken
            )
            .ConfigureAwait(false);

        // Update local state
        this.Scan.CurrentStage = stage;
        this.Scan.ResumeCursor = cursor;
        this.Scan.CheckpointVersion = newVersion;
        this.Scan.LastCheckpointAt = DateTime.UtcNow;

        // Update checkpoint for resumption
        this.Checkpoint = new ScanCheckpoint(stage, cursor, newVersion);
    }

    /// <inheritdoc />
    public async ValueTask RecordSeenPathsAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken
    )
    {
        if (paths.Count == 0)
        {
            return;
        }

        await using var context = await this
            .dbContextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var seenPaths = paths
            .Select(p => new LibraryScanSeenPath
            {
                LibraryScanId = this.Scan.Id,
                FilePath = p,
                SeenAt = now,
            })
            .ToList();

        await context
            .BulkInsertOrUpdateAsync(
                seenPaths,
                new BulkConfig
                {
                    SetOutputIdentity = false,
                    UpdateByProperties =
                    [
                        nameof(LibraryScanSeenPath.LibraryScanId),
                        nameof(LibraryScanSeenPath.FilePath),
                    ],
                    PropertiesToIncludeOnUpdate = [nameof(LibraryScanSeenPath.SeenAt)],
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ClearCheckpointAsync(CancellationToken cancellationToken)
    {
        await using var context = await this
            .dbContextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        // Clear seen paths for this scan
        await context
            .LibraryScanSeenPaths.Where(p => p.LibraryScanId == this.Scan.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        // Clear checkpoint fields
        await context
            .LibraryScans.Where(s => s.Id == this.Scan.Id)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.CurrentStage, (string?)null)
                        .SetProperty(x => x.ResumeCursor, (string?)null)
                        .SetProperty(x => x.LastCheckpointAt, (DateTime?)null)
                        .SetProperty(x => x.CheckpointVersion, 0),
                cancellationToken
            )
            .ConfigureAwait(false);

        // Update local state
        this.Scan.CurrentStage = null;
        this.Scan.ResumeCursor = null;
        this.Scan.LastCheckpointAt = null;
        this.Scan.CheckpointVersion = 0;
        this.Checkpoint = null;
    }
}
