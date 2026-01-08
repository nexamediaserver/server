// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents an active or completed transcoding job bound to a playback session.
/// </summary>
public class TranscodeJob : AuditableEntity
{
    /// <summary>
    /// Gets or sets the playback session identifier that owns this job.
    /// </summary>
    public int PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the associated playback session.
    /// </summary>
    public PlaybackSession PlaybackSession { get; set; } = null!;

    /// <summary>
    /// Gets or sets the media part identifier being transcoded.
    /// </summary>
    public int MediaPartId { get; set; }

    /// <summary>
    /// Gets or sets the associated media part.
    /// </summary>
    public MediaPart MediaPart { get; set; } = null!;

    /// <summary>
    /// Gets or sets the streaming protocol used for this job (dash, hls).
    /// </summary>
    public string Protocol { get; set; } = "dash";

    /// <summary>
    /// Gets or sets the FFmpeg process identifier for monitoring.
    /// </summary>
    public int? FfmpegProcessId { get; set; }

    /// <summary>
    /// Gets or sets the current job state.
    /// </summary>
    public TranscodeJobState State { get; set; } = TranscodeJobState.Pending;

    /// <summary>
    /// Gets or sets the transcode progress as a percentage (0-100).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the base output path for transcoded segments.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional seek offset in milliseconds when seeking mid-stream.
    /// </summary>
    public long? SeekOffsetMs { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate for this job.
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target video width for this job.
    /// </summary>
    public int? VideoWidth { get; set; }

    /// <summary>
    /// Gets or sets the target video height for this job.
    /// </summary>
    public int? VideoHeight { get; set; }

    /// <summary>
    /// Gets or sets the target audio bitrate for this job.
    /// </summary>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio channel count for this job.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the selected audio stream index.
    /// </summary>
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected subtitle stream index (for burn-in).
    /// </summary>
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hardware acceleration is enabled.
    /// </summary>
    public bool UseHardwareAcceleration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping should be applied.
    /// </summary>
    public bool EnableToneMapping { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job was last pinged (heartbeat).
    /// </summary>
    public DateTime LastPingAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the job started encoding.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job completed or was cancelled.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
