// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services.Pipeline;

namespace NexaMediaServer.Infrastructure.Services.Pipeline;

/// <summary>
/// Concrete pipeline context used during scans.
/// </summary>
public sealed class ScanPipelineContext : IPipelineContext
{
    private readonly Func<ScanPipelineProgress, CancellationToken, ValueTask>? progressReporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanPipelineContext"/> class.
    /// </summary>
    /// <param name="librarySection">The library section being scanned.</param>
    /// <param name="scan">The scan record.</param>
    /// <param name="progressReporter">Optional callback for progress reporting.</param>
    public ScanPipelineContext(
        LibrarySection librarySection,
        LibraryScan scan,
        Func<ScanPipelineProgress, CancellationToken, ValueTask>? progressReporter = null
    )
    {
        this.LibrarySection =
            librarySection ?? throw new ArgumentNullException(nameof(librarySection));
        this.Scan = scan ?? throw new ArgumentNullException(nameof(scan));
        this.progressReporter = progressReporter;
    }

    /// <inheritdoc />
    public LibrarySection LibrarySection { get; }

    /// <inheritdoc />
    public LibraryScan Scan { get; }

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
}
