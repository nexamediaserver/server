// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Linq;

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input type describing client playback capabilities.
/// </summary>
public sealed class PlaybackCapabilitiesInput
{
    /// <summary>
    /// Gets or sets the maximum streaming bitrate (bits per second).
    /// </summary>
    public int? MaxStreamingBitrate { get; set; }

    /// <summary>
    /// Gets or sets the maximum static download bitrate (bits per second).
    /// </summary>
    public int? MaxStaticBitrate { get; set; }

    /// <summary>
    /// Gets or sets the preferred music transcoding bitrate (bits per second).
    /// </summary>
    public int? MusicStreamingTranscodingBitrate { get; set; }

    /// <summary>
    /// Gets or sets the direct-play formats the client can handle.
    /// </summary>
    public List<DirectPlayProfileInput> DirectPlayProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the acceptable transcoding targets.
    /// </summary>
    public List<TranscodingProfileInput> TranscodingProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets container-level constraints.
    /// </summary>
    public List<ContainerProfileInput> ContainerProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets codec-specific constraints.
    /// </summary>
    public List<CodecProfileInput> CodecProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets subtitle delivery capabilities.
    /// </summary>
    public List<SubtitleProfileInput> SubtitleProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets response overrides for certain media types.
    /// </summary>
    public List<ResponseProfileInput> ResponseProfiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the image formats the client can render without server-side resizing.
    /// </summary>
    public List<string> SupportedImageFormats { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether DASH playback is supported.
    /// </summary>
    public bool? SupportsDash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HLS playback is supported.
    /// </summary>
    public bool? SupportsHls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device can render HDR natively.
    /// </summary>
    public bool? SupportsHdr { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tone mapping is acceptable for the device.
    /// </summary>
    public bool? AllowToneMapping { get; set; }

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="PlaybackCapabilities"/> built from this input.</returns>
    internal PlaybackCapabilities ToDto()
    {
        var dto = new PlaybackCapabilities
        {
            MaxStreamingBitrate = this.MaxStreamingBitrate ?? 60_000_000,
            MaxStaticBitrate = this.MaxStaticBitrate ?? 100_000_000,
            MusicStreamingTranscodingBitrate = this.MusicStreamingTranscodingBitrate ?? 384_000,
            DirectPlayProfiles = this.DirectPlayProfiles.Select(p => p.ToDto()).ToList(),
            TranscodingProfiles = this.TranscodingProfiles.Select(p => p.ToDto()).ToList(),
            ContainerProfiles = this.ContainerProfiles.Select(p => p.ToDto()).ToList(),
            CodecProfiles = this.CodecProfiles.Select(p => p.ToDto()).ToList(),
            SubtitleProfiles = this.SubtitleProfiles.Select(p => p.ToDto()).ToList(),
            ResponseProfiles = this.ResponseProfiles.Select(p => p.ToDto()).ToList(),
            SupportsDash = this.SupportsDash ?? true,
            SupportsHls = this.SupportsHls ?? false,
            SupportsHdr = this.SupportsHdr ?? false,
            AllowToneMapping = this.AllowToneMapping ?? true,
            SupportedImageFormats = this.SupportedImageFormats
                .Select(Core.Playback.PlaybackFormatSelector.NormalizeExtension)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };

        return dto;
    }
}
