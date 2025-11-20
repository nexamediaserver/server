// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Constants;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a server-owned playlist generator for a playback session.
/// </summary>
public class PlaylistGenerator : AuditableEntity
{
    /// <summary>
    /// Gets or sets the public identifier for this playlist generator.
    /// </summary>
    public Guid PlaylistGeneratorId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the parent playback session identifier.
    /// </summary>
    public int PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the parent playback session.
    /// </summary>
    public PlaybackSession PlaybackSession { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized seed describing how this generator was built (e.g., playlist, smart filter).
    /// </summary>
    public string SeedJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the cursor position within the generator.
    /// </summary>
    public int Cursor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeat mode is enabled.
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled.
    /// </summary>
    public bool Shuffle { get; set; }

    /// <summary>
    /// Gets or sets the serialized shuffle state used to keep deterministic order.
    /// </summary>
    public string? ShuffleState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this generator expires due to inactivity.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);

    /// <summary>
    /// Gets or sets the maximum number of items to materialize per chunk.
    /// </summary>
    public int ChunkSize { get; set; } = PlaybackDefaults.PlaylistChunkSize;

    /// <summary>
    /// Gets or sets the collection of materialized playlist items.
    /// </summary>
    public ICollection<PlaylistGeneratorItem> Items { get; set; } =
        new List<PlaylistGeneratorItem>();
}
