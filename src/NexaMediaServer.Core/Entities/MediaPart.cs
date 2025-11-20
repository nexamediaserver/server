// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a physical file part of a media item.
/// A media item can consist of multiple parts (e.g., multi-part video files).
/// </summary>
public class MediaPart : SoftDeletableEntity
{
    /// <summary>
    /// Gets or sets the ID of the media item this part belongs to.
    /// </summary>
    public int MediaItemId { get; set; }

    /// <summary>
    /// Gets or sets the media item this part belongs to.
    /// </summary>
    public MediaItem MediaItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the hash of this media part for identification.
    /// </summary>
    /// <remarks>
    /// This is a SHA-1 hash represented as a hexadecimal string.
    /// </remarks>
    public string? Hash { get; set; }

    /// <summary>
    /// Gets or sets the file path of this media part.
    /// </summary>
    public string File { get; set; } = null!;

    /// <summary>
    /// Gets or sets the size of this media part in bytes.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the duration of this media part.
    /// </summary>
    /// <remarks>
    /// The format varies depending on the media type (e.g., seconds for audio/video, pages for documents).
    /// </remarks>
    public double? Duration { get; set; }
}
