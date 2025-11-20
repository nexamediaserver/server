// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a library scan operation that tracks the progress and results of scanning a media library.
/// </summary>
public class LibraryScan : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the library being scanned.
    /// </summary>
    public int LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library entity being scanned.
    /// </summary>
    public LibrarySection LibrarySection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the library scan.
    /// </summary>
    public LibraryScanStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the scan started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the scan completed, or null if still in progress.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of files found during the scan.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files processed during the scan.
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of items added during the scan.
    /// </summary>
    public int ItemsAdded { get; set; }

    /// <summary>
    /// Gets or sets the number of items updated during the scan.
    /// </summary>
    public int ItemsUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of items removed during the scan.
    /// </summary>
    public int ItemsRemoved { get; set; }

    /// <summary>
    /// Gets or sets an error message if the scan failed, or null if successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the current pipeline stage name for resumption (e.g., "directory_traversal", "resolve_items").
    /// </summary>
    public string? CurrentStage { get; set; }

    /// <summary>
    /// Gets or sets the cursor position within the current stage for resuming.
    /// For directory traversal: current directory path. For other stages: last processed file path.
    /// </summary>
    public string? ResumeCursor { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last checkpoint update.
    /// </summary>
    public DateTime? LastCheckpointAt { get; set; }

    /// <summary>
    /// Gets or sets the checkpoint version for optimistic concurrency.
    /// </summary>
    public int CheckpointVersion { get; set; }

    /// <summary>
    /// Gets or sets the collection of seen file paths for this scan (for orphan detection).
    /// </summary>
    public ICollection<LibraryScanSeenPath> SeenPaths { get; set; } =
        new List<LibraryScanSeenPath>();
}
