// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents metadata describing a media stream for a specific media item and part.
/// </summary>
public class MediaStreamEntity : AuditableEntity
{
    /// <summary>
    /// Gets or sets the type of stream (audio, video, subtitles, etc.).
    /// </summary>
    public StreamType StreamType { get; set; }

    /// <summary>
    /// Gets or sets the codec used to encode the stream.
    /// </summary>
    public string Codec { get; set; } = null!;

    /// <summary>
    /// Gets or sets the language of the stream, if applicable.
    /// </summary>
    public string Language { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of audio channels provided by the stream.
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Gets or sets the bitrate of the stream in bits per second.
    /// </summary>
    public int Bitrate { get; set; }

    /// <summary>
    /// Gets or sets the index of the stream within the media container.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this stream is marked as the default selection.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this stream is forced to play.
    /// </summary>
    public bool IsForced { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the media item that owns this stream.
    /// </summary>
    public int MediaItemId { get; set; }

    /// <summary>
    /// Gets or sets the media item navigation property.
    /// </summary>
    public MediaItem MediaItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the media part that contains this stream.
    /// </summary>
    public int MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the media part navigation property.
    /// </summary>
    public MediaPart MediaPart { get; set; } = null!;
}
