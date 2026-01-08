// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

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
    /// Gets or sets the media type this plan targets (Video, Audio, Photo).
    /// </summary>
    public string MediaType { get; set; } = "Video";

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

    /// <summary>
    /// Gets or sets the reasons why transcoding is required.
    /// </summary>
    public TranscodeReason TranscodeReasons { get; set; } = TranscodeReason.None;

    /// <summary>
    /// Gets or sets the external subtitle URL for sidecar delivery.
    /// </summary>
    public string? SubtitleUrl { get; set; }

    /// <summary>
    /// Gets or sets the subtitle delivery method (External, Encode, Embed, Drop).
    /// </summary>
    public string? SubtitleDeliveryMethod { get; set; }

    /// <summary>
    /// Gets or sets the selected audio track language.
    /// </summary>
    public string? AudioLanguage { get; set; }

    /// <summary>
    /// Gets or sets the selected subtitle track language.
    /// </summary>
    public string? SubtitleLanguage { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate for adaptive bitrate streaming.
    /// </summary>
    public int? TargetVideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target video width for scaling.
    /// </summary>
    public int? TargetVideoWidth { get; set; }

    /// <summary>
    /// Gets or sets the target video height for scaling.
    /// </summary>
    public int? TargetVideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the target audio bitrate for transcoding.
    /// </summary>
    public int? TargetAudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio channel count for downmixing.
    /// </summary>
    public int? TargetAudioChannels { get; set; }
}
