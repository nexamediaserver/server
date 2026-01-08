// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Declarative description of what a playback client can handle without transcoding.
/// </summary>
public sealed class PlaybackCapabilities
{
    /// <summary>
    /// Gets or sets the maximum sustained streaming bitrate the client expects (bits per second).
    /// </summary>
    public int MaxStreamingBitrate { get; set; } = 60_000_000;

    /// <summary>
    /// Gets or sets the maximum bitrate for static downloads (bits per second).
    /// </summary>
    public int MaxStaticBitrate { get; set; } = 100_000_000;

    /// <summary>
    /// Gets or sets the preferred music transcoding bitrate (bits per second).
    /// </summary>
    public int MusicStreamingTranscodingBitrate { get; set; } = 384_000;

    /// <summary>
    /// Gets or sets the formats the client can direct play without server processing.
    /// </summary>
    public List<DirectPlayProfile> DirectPlayProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the transcoding formats the client can accept.
    /// </summary>
    public List<TranscodingProfile> TranscodingProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets container-level conditions that further refine support.
    /// </summary>
    public List<ContainerProfile> ContainerProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets codec-level constraints for playback.
    /// </summary>
    public List<CodecProfile> CodecProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets subtitle handling capabilities.
    /// </summary>
    public List<SubtitleProfile> SubtitleProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets response format overrides (e.g., mime type hints).
    /// </summary>
    public List<ResponseProfile> ResponseProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the image formats the client can display without resizing performed server-side.
    /// </summary>
    public List<string> SupportedImageFormats { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether DASH manifests are supported.
    /// </summary>
    public bool SupportsDash { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether HLS manifests are supported.
    /// </summary>
    public bool SupportsHls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device can render HDR content natively.
    /// </summary>
    public bool SupportsHdr { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping is acceptable for this device.
    /// </summary>
    public bool AllowToneMapping { get; set; } = true;
}
