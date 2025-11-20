// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Tracks the state of events for a single path, allowing event coalescing.
/// </summary>
internal sealed class PathEventState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathEventState"/> class.
    /// </summary>
    /// <param name="initialEvent">The initial filesystem change event.</param>
    public PathEventState(FileSystemChangeEvent initialEvent)
    {
        this.LatestEvent = initialEvent;
        this.WasCreated = initialEvent.ChangeType == FileSystemChangeType.Created;
        this.IsDeleted = initialEvent.ChangeType == FileSystemChangeType.Deleted;
    }

    /// <summary>
    /// Gets the most recent event for this path.
    /// </summary>
    public FileSystemChangeEvent LatestEvent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this path was created during the debounce window.
    /// </summary>
    public bool WasCreated { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this path was deleted (most recently).
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the final action to take for this path after coalescing.
    /// </summary>
    public PathFinalAction FinalAction
    {
        get
        {
            if (this.WasCreated && this.IsDeleted)
            {
                // Created then deleted in same window - no action needed
                return PathFinalAction.Ignore;
            }

            if (this.IsDeleted)
            {
                return PathFinalAction.Remove;
            }

            // Created, Modified, or Renamed - need to scan
            return PathFinalAction.Scan;
        }
    }

    /// <summary>
    /// Merges a new event into this state.
    /// </summary>
    /// <param name="newEvent">The new event to merge.</param>
    /// <returns>The updated state.</returns>
    public PathEventState Merge(FileSystemChangeEvent newEvent)
    {
        this.LatestEvent = newEvent;

        switch (newEvent.ChangeType)
        {
            case FileSystemChangeType.Created:
                this.WasCreated = true;
                this.IsDeleted = false;
                break;

            case FileSystemChangeType.Deleted:
                this.IsDeleted = true;
                break;

            case FileSystemChangeType.Modified:
            case FileSystemChangeType.Renamed:
                this.IsDeleted = false;
                break;

            default:
                break;
        }

        return this;
    }

    /// <summary>
    /// Marks this path as deleted.
    /// </summary>
    /// <returns>The updated state.</returns>
    public PathEventState MarkDeleted()
    {
        this.IsDeleted = true;
        return this;
    }
}
