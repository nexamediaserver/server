// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides information about FFmpeg binary capabilities detected at runtime.
/// </summary>
public interface IFfmpegCapabilities
{
    /// <summary>
    /// Gets the FFmpeg version string.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the set of supported hardware acceleration types.
    /// </summary>
    IReadOnlySet<HardwareAccelerationKind> SupportedHwAccel { get; }

    /// <summary>
    /// Gets the set of available encoder names (e.g., "h264_nvenc", "hevc_qsv").
    /// </summary>
    IReadOnlySet<string> SupportedEncoders { get; }

    /// <summary>
    /// Gets the set of available filter names (e.g., "scale_cuda", "tonemap_vaapi").
    /// </summary>
    IReadOnlySet<string> SupportedFilters { get; }

    /// <summary>
    /// Gets the set of available decoder names (e.g., "h264_cuvid", "hevc_qsv").
    /// </summary>
    IReadOnlySet<string> SupportedDecoders { get; }

    /// <summary>
    /// Gets the recommended hardware acceleration for the current platform.
    /// </summary>
    HardwareAccelerationKind RecommendedAcceleration { get; }

    /// <summary>
    /// Gets a value indicating whether FFmpeg capabilities have been detected.
    /// </summary>
    bool IsDetected { get; }

    /// <summary>
    /// Checks if a specific encoder is supported.
    /// </summary>
    /// <param name="encoderName">The encoder name (e.g., "h264_nvenc").</param>
    /// <returns>True if the encoder is supported; otherwise, false.</returns>
    bool SupportsEncoder(string encoderName);

    /// <summary>
    /// Checks if a specific filter is supported.
    /// </summary>
    /// <param name="filterName">The filter name (e.g., "scale_cuda").</param>
    /// <returns>True if the filter is supported; otherwise, false.</returns>
    bool SupportsFilter(string filterName);

    /// <summary>
    /// Checks if a specific decoder is supported.
    /// </summary>
    /// <param name="decoderName">The decoder name (e.g., "h264_cuvid").</param>
    /// <returns>True if the decoder is supported; otherwise, false.</returns>
    bool SupportsDecoder(string decoderName);

    /// <summary>
    /// Checks if hardware decoding is available for the given codec and acceleration type.
    /// Supports H.264, HEVC, and AV1 codecs.
    /// </summary>
    /// <param name="codec">The codec name (e.g., "h264", "hevc", "av1").</param>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>True if hardware decoding is available; otherwise, false.</returns>
    bool IsHardwareDecoderAvailable(string codec, HardwareAccelerationKind kind);

    /// <summary>
    /// Checks if a specific hardware acceleration type is supported.
    /// </summary>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>True if the acceleration type is supported; otherwise, false.</returns>
    bool SupportsHwAccel(HardwareAccelerationKind kind);
}
