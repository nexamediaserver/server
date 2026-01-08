// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Payload containing server-wide configuration settings.
/// </summary>
/// <param name="ServerName">The friendly display name of the server.</param>
/// <param name="MaxStreamingBitrate">Maximum streaming bitrate in bits per second.</param>
/// <param name="PreferH265">Whether to prefer H.265 (HEVC) codec for video transcoding.</param>
/// <param name="AllowRemuxing">Whether to allow remuxing (container change without re-encoding).</param>
/// <param name="AllowHEVCEncoding">Whether to allow HEVC encoding when transcoding video.</param>
/// <param name="DashVideoCodec">Default video codec for DASH transcoding.</param>
/// <param name="DashAudioCodec">Default audio codec for DASH transcoding.</param>
/// <param name="DashSegmentDurationSeconds">DASH segment duration in seconds.</param>
/// <param name="EnableToneMapping">Whether tone mapping is enabled for HDR content.</param>
/// <param name="UserPreferredAcceleration">User's preferred hardware acceleration (null = auto-detect).</param>
/// <param name="AllowedTags">List of allowed tags (empty = no allowlist).</param>
/// <param name="BlockedTags">List of blocked tags (empty = no blocklist).</param>
/// <param name="GenreMappings">Genre normalization mappings (input → canonical).</param>
/// <param name="LogLevel">Minimum log level (Debug, Information, Warning, Error, Fatal).</param>
public sealed record ServerSettingsPayload(
    string ServerName,
    int MaxStreamingBitrate,
    bool PreferH265,
    bool AllowRemuxing,
    bool AllowHEVCEncoding,
    string DashVideoCodec,
    string DashAudioCodec,
    int DashSegmentDurationSeconds,
    bool EnableToneMapping,
    HardwareAccelerationKind? UserPreferredAcceleration,
    List<string> AllowedTags,
    List<string> BlockedTags,
    Dictionary<string, string> GenreMappings,
    string LogLevel);
