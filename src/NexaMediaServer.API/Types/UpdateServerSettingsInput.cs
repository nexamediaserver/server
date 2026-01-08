// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for updating server-wide configuration settings.
/// All fields are optional; only specified fields will be updated.
/// </summary>
public sealed record UpdateServerSettingsInput
{
    /// <summary>
    /// Gets the friendly display name of the server.
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// Gets the maximum streaming bitrate in bits per second.
    /// </summary>
    public int? MaxStreamingBitrate { get; init; }

    /// <summary>
    /// Gets a value indicating whether to prefer H.265 (HEVC) codec for video transcoding.
    /// </summary>
    public bool? PreferH265 { get; init; }

    /// <summary>
    /// Gets a value indicating whether to allow remuxing (container change without re-encoding).
    /// </summary>
    public bool? AllowRemuxing { get; init; }

    /// <summary>
    /// Gets a value indicating whether to allow HEVC encoding when transcoding video.
    /// </summary>
    public bool? AllowHEVCEncoding { get; init; }

    /// <summary>
    /// Gets the default video codec for DASH transcoding.
    /// </summary>
    public string? DashVideoCodec { get; init; }

    /// <summary>
    /// Gets the default audio codec for DASH transcoding.
    /// </summary>
    public string? DashAudioCodec { get; init; }

    /// <summary>
    /// Gets the DASH segment duration in seconds.
    /// </summary>
    public int? DashSegmentDurationSeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether tone mapping is enabled for HDR content.
    /// </summary>
    public bool? EnableToneMapping { get; init; }

    /// <summary>
    /// Gets the user's preferred hardware acceleration (null = auto-detect).
    /// </summary>
    public HardwareAccelerationKind? UserPreferredAcceleration { get; init; }

    /// <summary>
    /// Gets the list of allowed tags (empty = no allowlist).
    /// </summary>
    public List<string>? AllowedTags { get; init; }

    /// <summary>
    /// Gets the list of blocked tags (empty = no blocklist).
    /// </summary>
    public List<string>? BlockedTags { get; init; }

    /// <summary>
    /// Gets the genre normalization mappings (input → canonical).
    /// </summary>
    public Dictionary<string, string>? GenreMappings { get; init; }

    /// <summary>
    /// Gets the minimum log level (Debug, Information, Warning, Error, Fatal).
    /// </summary>
    public string? LogLevel { get; init; }
}
