// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Describes how a media part should be delivered to a client.
/// </summary>
public sealed class PlaybackStreamPlan
{
    /// <summary>
    /// Gets or sets the playback delivery mode.
    /// </summary>
    public PlaybackMode Mode { get; set; } = PlaybackMode.DirectPlay;

    /// <summary>
    /// Gets or sets the transport protocol (progressive, dash, hls).
    /// </summary>
    public string Protocol { get; set; } = "progressive";

    /// <summary>
    /// Gets or sets the media part identifier.
    /// </summary>
    public int MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the container used for delivery.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direct URL (progressive download / direct play).
    /// </summary>
    public string? DirectUrl { get; set; }

    /// <summary>
    /// Gets or sets the remux URL for direct streaming without re-encoding.
    /// </summary>
    public string? RemuxUrl { get; set; }

    /// <summary>
    /// Gets or sets the manifest URL for segmented streaming (e.g., DASH).
    /// </summary>
    public string? ManifestUrl { get; set; }

    /// <summary>
    /// Gets or sets the selected video stream index, if known.
    /// </summary>
    public int? VideoStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected audio stream index, if known.
    /// </summary>
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected subtitle stream index, if known.
    /// </summary>
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the desired video codec after planning.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the desired audio codec after planning.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the desired subtitle codec after planning.
    /// </summary>
    public string? SubtitleCodec { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video stream should be copied.
    /// </summary>
    public bool CopyVideo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio stream should be copied.
    /// </summary>
    public bool CopyAudio { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping should be applied.
    /// </summary>
    public bool EnableToneMapping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hardware acceleration is requested.
    /// </summary>
    public bool UseHardwareAcceleration { get; set; }
}
