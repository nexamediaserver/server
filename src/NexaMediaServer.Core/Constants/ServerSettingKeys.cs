// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Defines the keys used to store server settings in the database.
/// </summary>
public static class ServerSettingKeys
{
    /// <summary>
    /// The friendly display name of the server.
    /// </summary>
    public const string ServerName = "ServerName";

    /// <summary>
    /// Maximum streaming bitrate in bits per second.
    /// </summary>
    public const string MaxStreamingBitrate = "MaxStreamingBitrate";

    /// <summary>
    /// Whether to prefer H.265 (HEVC) codec for video transcoding.
    /// </summary>
    public const string PreferH265 = "PreferH265";

    /// <summary>
    /// Whether to allow remuxing (container change without re-encoding).
    /// </summary>
    public const string AllowRemuxing = "AllowRemuxing";

    /// <summary>
    /// Whether to allow HEVC encoding when transcoding video.
    /// </summary>
    public const string AllowHEVCEncoding = "AllowHEVCEncoding";

    /// <summary>
    /// Default video codec for DASH transcoding.
    /// </summary>
    public const string DashVideoCodec = "DashVideoCodec";

    /// <summary>
    /// Default audio codec for DASH transcoding.
    /// </summary>
    public const string DashAudioCodec = "DashAudioCodec";

    /// <summary>
    /// DASH segment duration in seconds.
    /// </summary>
    public const string DashSegmentDurationSeconds = "DashSegmentDurationSeconds";

    /// <summary>
    /// Whether tone mapping is enabled for HDR content.
    /// </summary>
    public const string EnableToneMapping = "EnableToneMapping";

    /// <summary>
    /// User's preferred hardware acceleration (null = auto-detect).
    /// </summary>
    public const string UserPreferredAcceleration = "UserPreferredAcceleration";

    /// <summary>
    /// List of allowed tags (JSON array). If populated, only these tags are allowed.
    /// </summary>
    public const string AllowedTags = "AllowedTags";

    /// <summary>
    /// List of blocked tags (JSON array). Only used when AllowedTags is empty.
    /// </summary>
    public const string BlockedTags = "BlockedTags";

    /// <summary>
    /// Genre normalization mappings (JSON object). Maps input genre names to canonical forms.
    /// </summary>
    public const string GenreMappings = "GenreMappings";

    /// <summary>
    /// Minimum log level for Serilog (Debug, Information, Warning, Error, Fatal).
    /// </summary>
    public const string LogLevel = "LogLevel";
}
