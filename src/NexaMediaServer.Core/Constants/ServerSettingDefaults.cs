// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Provides default values for server settings when no value is stored in the database
/// or configured via IConfiguration.
/// </summary>
public static class ServerSettingDefaults
{
    /// <summary>
    /// Default server display name.
    /// </summary>
    public const string ServerName = "Nexa Media Server";

    /// <summary>
    /// Default maximum streaming bitrate (60 Mbps).
    /// </summary>
    public const int MaxStreamingBitrate = 60_000_000;

    /// <summary>
    /// Default preference for H.265 codec.
    /// </summary>
    public const bool PreferH265 = false;

    /// <summary>
    /// Default setting for allowing remuxing.
    /// </summary>
    public const bool AllowRemuxing = true;

    /// <summary>
    /// Default setting for allowing HEVC encoding.
    /// </summary>
    public const bool AllowHEVCEncoding = true;

    /// <summary>
    /// Default video codec for DASH transcoding.
    /// </summary>
    public const string DashVideoCodec = "h264";

    /// <summary>
    /// Default audio codec for DASH transcoding.
    /// </summary>
    public const string DashAudioCodec = "aac";

    /// <summary>
    /// Default DASH segment duration in seconds.
    /// </summary>
    public const int DashSegmentDurationSeconds = 4;

    /// <summary>
    /// Default tone mapping enabled state.
    /// </summary>
    public const bool EnableToneMapping = false;

    /// <summary>
    /// Default user preferred hardware acceleration (null = auto-detect).
    /// </summary>
    public const string? UserPreferredAcceleration = null;

    /// <summary>
    /// Default allowed tags list (empty = no allowlist).
    /// </summary>
    public const string AllowedTags = "[]";

    /// <summary>
    /// Default blocked tags list (empty = no blocklist).
    /// </summary>
    public const string BlockedTags = "[]";

    /// <summary>
    /// Default genre normalization mappings.
    /// </summary>
    public const string GenreMappings = "{\"Sci-Fi\":\"Science Fiction\",\"SciFi\":\"Science Fiction\",\"Sci Fi\":\"Science Fiction\",\"Doc\":\"Documentary\",\"Docu\":\"Documentary\",\"Anime\":\"Animation\",\"Cartoon\":\"Animation\",\"R&B\":\"Rhythm and Blues\",\"Hip-Hop\":\"Hip Hop\",\"Rap\":\"Hip Hop\"}";

    /// <summary>
    /// Default minimum log level.
    /// </summary>
    public const string LogLevel = "Information";
}
