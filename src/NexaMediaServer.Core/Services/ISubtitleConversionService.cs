// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for converting between subtitle formats.
/// </summary>
public interface ISubtitleConversionService
{
    /// <summary>
    /// Converts a subtitle file to the specified output format.
    /// </summary>
    /// <param name="inputPath">Path to the source subtitle file.</param>
    /// <param name="outputFormat">Target format (e.g., "vtt", "srt", "ass").</param>
    /// <param name="startPositionTicks">Optional start position in ticks to filter cues.</param>
    /// <param name="endPositionTicks">Optional end position in ticks to filter cues.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream containing the converted subtitle content.</returns>
    Task<Stream> ConvertAsync(
        string inputPath,
        string outputFormat,
        long? startPositionTicks = null,
        long? endPositionTicks = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a subtitle stream to the specified output format.
    /// </summary>
    /// <param name="inputStream">The source subtitle stream.</param>
    /// <param name="inputFormat">The format of the input stream (e.g., "srt", "ass").</param>
    /// <param name="outputFormat">Target format (e.g., "vtt", "srt", "ass").</param>
    /// <param name="startPositionTicks">Optional start position in ticks to filter cues.</param>
    /// <param name="endPositionTicks">Optional end position in ticks to filter cues.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream containing the converted subtitle content.</returns>
    Task<Stream> ConvertAsync(
        Stream inputStream,
        string inputFormat,
        string outputFormat,
        long? startPositionTicks = null,
        long? endPositionTicks = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a subtitle stream from a media file using FFmpeg.
    /// </summary>
    /// <param name="mediaPath">Path to the media file.</param>
    /// <param name="streamIndex">The subtitle stream index within the container.</param>
    /// <param name="outputFormat">Target format for extraction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream containing the extracted subtitle content.</returns>
    Task<Stream> ExtractFromMediaAsync(
        string mediaPath,
        int streamIndex,
        string outputFormat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the MIME type for a subtitle format.
    /// </summary>
    /// <param name="format">The subtitle format.</param>
    /// <returns>The appropriate MIME type.</returns>
    string GetMimeType(string format);

    /// <summary>
    /// Determines if the format requires FFmpeg extraction (image-based subtitles).
    /// </summary>
    /// <param name="codec">The subtitle codec.</param>
    /// <returns>True if FFmpeg is required for extraction.</returns>
    bool RequiresFfmpegExtraction(string codec);
}
