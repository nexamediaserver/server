// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FFMpegCore;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Services;

using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Subtitle conversion service using libse (SubtitleEdit library).
/// </summary>
public sealed partial class SubtitleConversionService : ISubtitleConversionService
{
    private static readonly string[] ImageBasedCodecs =
    [
        "hdmv_pgs_subtitle",
        "pgssub",
        "pgs",
        "dvdsub",
        "dvd_subtitle",
        "vobsub",
        "xsub",
    ];

    private readonly ILogger<SubtitleConversionService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleConversionService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SubtitleConversionService(ILogger<SubtitleConversionService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<Stream> ConvertAsync(
        string inputPath,
        string outputFormat,
        long? startPositionTicks = null,
        long? endPositionTicks = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFormat);

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Subtitle file not found.", inputPath);
        }

        var text = await File.ReadAllTextAsync(inputPath, cancellationToken).ConfigureAwait(false);
        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var inputFormat = Path.GetExtension(inputPath).TrimStart('.').ToLowerInvariant();

        return await this.ConvertAsync(
            inputStream,
            inputFormat,
            outputFormat,
            startPositionTicks,
            endPositionTicks,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Stream> ConvertAsync(
        Stream inputStream,
        string inputFormat,
        string outputFormat,
        long? startPositionTicks = null,
        long? endPositionTicks = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFormat);

        cancellationToken.ThrowIfCancellationRequested();

        // Read input stream
        using var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = reader.ReadToEnd().Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();

        // Find input format
        var sourceFormat = FindFormat(inputFormat, lines);
        if (sourceFormat == null)
        {
            this.LogUnknownInputFormat(inputFormat);
            throw new NotSupportedException($"Unknown subtitle input format: {inputFormat}");
        }

        // Parse subtitle
        var subtitle = new Subtitle();
        sourceFormat.LoadSubtitle(subtitle, lines, null);

        // Filter by time range if specified
        if (startPositionTicks.HasValue || endPositionTicks.HasValue)
        {
            FilterByTimeRange(subtitle, startPositionTicks, endPositionTicks);
        }

        // Find output format
        var targetFormat = FindFormat(outputFormat, null);
        if (targetFormat == null)
        {
            this.LogUnknownOutputFormat(outputFormat);
            throw new NotSupportedException($"Unknown subtitle output format: {outputFormat}");
        }

        // Remove format-specific formatting when converting
        sourceFormat.RemoveNativeFormatting(subtitle, targetFormat);

        // Convert to output format
        var outputText = targetFormat.ToText(subtitle, string.Empty);
        var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(outputText));

        this.LogConversionSuccess(inputFormat, outputFormat, subtitle.Paragraphs.Count);

        return Task.FromResult<Stream>(outputStream);
    }

    /// <inheritdoc />
    public async Task<Stream> ExtractFromMediaAsync(
        string mediaPath,
        int streamIndex,
        string outputFormat,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFormat);

        if (!File.Exists(mediaPath))
        {
            throw new FileNotFoundException("Media file not found.", mediaPath);
        }

        var tempFile = Path.Combine(
            Path.GetTempPath(),
            $"nexa_sub_{Guid.NewGuid():N}.{outputFormat}");

        try
        {
            // Use FFmpeg to extract subtitle stream
            var ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();
            var args = $"-i \"{mediaPath}\" -map 0:s:{streamIndex} -c:s {GetFfmpegCodec(outputFormat)} \"{tempFile}\"";

            this.LogExtractingSubtitle(mediaPath, streamIndex, outputFormat);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                },
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                this.LogFfmpegExtractionFailed(process.ExitCode, error);
                throw new InvalidOperationException($"FFmpeg subtitle extraction failed: {error}");
            }

            // Read the extracted file into memory
            var bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken).ConfigureAwait(false);
            return new MemoryStream(bytes);
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <inheritdoc />
    public string GetMimeType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "vtt" or "webvtt" => "text/vtt",
            "srt" or "subrip" => "text/plain; charset=utf-8",
            "ass" or "ssa" => "text/x-ssa",
            "ttml" => "application/ttml+xml",
            "json" => "application/json",
            _ => "text/plain",
        };
    }

    /// <inheritdoc />
    public bool RequiresFfmpegExtraction(string codec)
    {
        return ImageBasedCodecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
    }

    private static SubtitleFormat? FindFormat(string formatName, System.Collections.Generic.List<string>? lines)
    {
        var normalized = formatName.ToLowerInvariant();

        // Map common format names to SubtitleEdit format classes
        SubtitleFormat? format = normalized switch
        {
            "vtt" or "webvtt" => new WebVTT(),
            "srt" or "subrip" => new SubRip(),
            "ass" => new AdvancedSubStationAlpha(),
            "ssa" => new SubStationAlpha(),
            "ttml" => new TimedText10(),
            "smi" or "sami" => new Sami(),
            "sub" => new SubViewer20(),
            _ => null,
        };

        // If we have lines, verify the format matches
        if (format != null && lines != null && lines.Count > 0)
        {
            if (format.IsMine(lines, null))
            {
                return format;
            }

            // Try auto-detection from all formats
            var detectedFormat = SubtitleFormat.AllSubtitleFormats
                .FirstOrDefault(candidate => candidate.IsMine(lines, null));

            if (detectedFormat != null)
            {
                return detectedFormat;
            }
        }

        return format;
    }

    private static string GetFfmpegCodec(string outputFormat)
    {
        return outputFormat.ToLowerInvariant() switch
        {
            "vtt" or "webvtt" => "webvtt",
            "srt" or "subrip" => "srt",
            "ass" => "ass",
            "ssa" => "ssa",
            _ => "srt", // Default to SRT for unknown formats
        };
    }

    private static void FilterByTimeRange(Subtitle subtitle, long? startTicks, long? endTicks)
    {
        var startMs = startTicks.HasValue
            ? startTicks.Value / TimeSpan.TicksPerMillisecond
            : 0;

        var endMs = endTicks.HasValue
            ? endTicks.Value / TimeSpan.TicksPerMillisecond
            : double.MaxValue;

        // Filter paragraphs by time range
        var paragraphsToRemove = subtitle.Paragraphs
            .Where(p => p.EndTime.TotalMilliseconds < startMs || p.StartTime.TotalMilliseconds > endMs)
            .ToList();

        foreach (var p in paragraphsToRemove)
        {
            subtitle.Paragraphs.Remove(p);
        }

        // Adjust timing if start position is specified
        if (startTicks.HasValue && startMs > 0)
        {
            foreach (var p in subtitle.Paragraphs)
            {
                p.StartTime.TotalMilliseconds -= startMs;
                p.EndTime.TotalMilliseconds -= startMs;

                // Clamp negative values
                if (p.StartTime.TotalMilliseconds < 0)
                {
                    p.StartTime.TotalMilliseconds = 0;
                }
            }
        }

        subtitle.Renumber();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown subtitle input format: {Format}")]
    private partial void LogUnknownInputFormat(string format);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown subtitle output format: {Format}")]
    private partial void LogUnknownOutputFormat(string format);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Converted subtitle from {InputFormat} to {OutputFormat} ({CueCount} cues)")]
    private partial void LogConversionSuccess(string inputFormat, string outputFormat, int cueCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Extracting subtitle from {MediaPath} stream {StreamIndex} to {Format}")]
    private partial void LogExtractingSubtitle(string mediaPath, int streamIndex, string format);

    [LoggerMessage(Level = LogLevel.Error, Message = "FFmpeg subtitle extraction failed with exit code {ExitCode}: {Error}")]
    private partial void LogFfmpegExtractionFailed(int exitCode, string error);
}
