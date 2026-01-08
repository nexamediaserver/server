// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using FFMpegCore;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg;

/// <summary>
/// Detects FFmpeg capabilities on server startup and populates the IFfmpegCapabilities singleton.
/// </summary>
public sealed partial class FFmpegCapabilitiesService : IFfmpegCapabilities, IHostedService
{
    private readonly ILogger<FFmpegCapabilitiesService> logger;
    private readonly IOptionsMonitor<TranscodeOptions> transcodeOptions;
    private readonly HashSet<HardwareAccelerationKind> supportedHwAccel = new();
    private readonly HashSet<string> supportedEncoders = new();
    private readonly HashSet<string> supportedFilters = new();
    private readonly HashSet<string> supportedDecoders = new();
    private string version = string.Empty;
    private HardwareAccelerationKind recommendedAcceleration = HardwareAccelerationKind.None;
    private bool isDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegCapabilitiesService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="transcodeOptions">The transcode options.</param>
    public FFmpegCapabilitiesService(
        ILogger<FFmpegCapabilitiesService> logger,
        IOptionsMonitor<TranscodeOptions> transcodeOptions)
    {
        this.logger = logger;
        this.transcodeOptions = transcodeOptions;
    }

    /// <inheritdoc/>
    public string Version => this.version;

    /// <inheritdoc/>
    public IReadOnlySet<HardwareAccelerationKind> SupportedHwAccel => this.supportedHwAccel;

    /// <inheritdoc/>
    public IReadOnlySet<string> SupportedEncoders => this.supportedEncoders;

    /// <inheritdoc/>
    public IReadOnlySet<string> SupportedFilters => this.supportedFilters;

    /// <inheritdoc/>
    public IReadOnlySet<string> SupportedDecoders => this.supportedDecoders;

    /// <inheritdoc/>
    public HardwareAccelerationKind RecommendedAcceleration => this.recommendedAcceleration;

    /// <inheritdoc/>
    public bool IsDetected => this.isDetected;

    /// <inheritdoc/>
    public bool SupportsEncoder(string encoderName) =>
        this.supportedEncoders.Contains(encoderName, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool SupportsFilter(string filterName) =>
        this.supportedFilters.Contains(filterName, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool SupportsDecoder(string decoderName) =>
        this.supportedDecoders.Contains(decoderName, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool IsHardwareDecoderAvailable(string codec, HardwareAccelerationKind kind)
    {
        if (kind == HardwareAccelerationKind.None || !this.SupportsHwAccel(kind))
        {
            return false;
        }

        var decoderName = GetHardwareDecoderName(codec, kind);
        return decoderName != null && this.SupportsDecoder(decoderName);
    }

    /// <inheritdoc/>
    public bool SupportsHwAccel(HardwareAccelerationKind kind) =>
        this.supportedHwAccel.Contains(kind);

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogDetectingCapabilities(this.logger);

        try
        {
            var ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();

            // Detect version
            this.version = await DetectVersionAsync(ffmpegPath, cancellationToken);
            LogFFmpegVersion(this.logger, this.version);

            // Detect hardware accelerators
            await this.DetectHardwareAcceleratorsAsync(ffmpegPath, cancellationToken);
            LogDetectedHwAccel(this.logger, string.Join(", ", this.supportedHwAccel));

            // Detect encoders
            await this.DetectEncodersAsync(ffmpegPath, cancellationToken);
            LogDetectedEncoders(this.logger, this.supportedEncoders.Count);

            // Detect decoders
            await this.DetectDecodersAsync(ffmpegPath, cancellationToken);
            LogDetectedDecoders(this.logger, this.supportedDecoders.Count);

            // Detect filters
            await this.DetectFiltersAsync(ffmpegPath, cancellationToken);
            LogDetectedFilters(this.logger, this.supportedFilters.Count);

            // Determine recommended acceleration for platform
            this.DetermineRecommendedAcceleration();
            LogRecommendedAcceleration(this.logger, this.recommendedAcceleration);

            // Update transcode options with detection results
            this.UpdateTranscodeOptions();

            this.isDetected = true;
            LogCapabilitiesDetected(this.logger);
        }
        catch (Exception ex)
        {
            LogCapabilityDetectionFailed(this.logger, ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<string> RunFFmpegCommandAsync(
        string ffmpegPath,
        string arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return output;
    }

    private static async Task<string> DetectVersionAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFFmpegCommandAsync(ffmpegPath, "-version", cancellationToken);

        // Extract version from first line: "ffmpeg version N-123456-gabcdef Copyright..."
        var match = Regex.Match(output, @"ffmpeg version ([^\s]+)");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private static string? GetHardwareDecoderName(string codec, HardwareAccelerationKind kind)
    {
        // Only support H.264, HEVC, and AV1 for hardware decoding
        return kind switch
        {
            HardwareAccelerationKind.Nvenc => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_cuvid",
                "hevc" or "h265" => "hevc_cuvid",
                "av1" => "av1_cuvid",
                _ => null
            },
            HardwareAccelerationKind.Qsv => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_qsv",
                "hevc" or "h265" => "hevc_qsv",
                "av1" => "av1_qsv",
                _ => null
            },
            HardwareAccelerationKind.Vaapi => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_vaapi",
                "hevc" or "h265" => "hevc_vaapi",
                "av1" => "av1_vaapi",
                _ => null
            },
            HardwareAccelerationKind.VideoToolbox => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_videotoolbox",
                "hevc" or "h265" => "hevc_videotoolbox",
                _ => null // VideoToolbox doesn't have AV1 decoder
            },
            HardwareAccelerationKind.Amf => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_amf",
                "hevc" or "h265" => "hevc_amf",
                "av1" => "av1_amf",
                _ => null
            },
            HardwareAccelerationKind.Rkmpp => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_rkmpp",
                "hevc" or "h265" => "hevc_rkmpp",
                _ => null
            },
            HardwareAccelerationKind.V4L2M2M => codec.ToLowerInvariant() switch
            {
                "h264" => "h264_v4l2m2m",
                "hevc" or "h265" => "hevc_v4l2m2m",
                _ => null
            },
            _ => null
        };
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Detecting FFmpeg capabilities...")]
    private static partial void LogDetectingCapabilities(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "FFmpeg version: {Version}")]
    private static partial void LogFFmpegVersion(ILogger logger, string version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Detected hardware acceleration: {HwAccel}")]
    private static partial void LogDetectedHwAccel(ILogger logger, string hwAccel);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Detected {Count} encoders")]
    private static partial void LogDetectedEncoders(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Detected {Count} decoders")]
    private static partial void LogDetectedDecoders(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Detected {Count} filters")]
    private static partial void LogDetectedFilters(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Recommended hardware acceleration for this platform: {Acceleration}")]
    private static partial void LogRecommendedAcceleration(ILogger logger, HardwareAccelerationKind acceleration);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "FFmpeg capabilities detection completed")]
    private static partial void LogCapabilitiesDetected(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to detect FFmpeg capabilities")]
    private static partial void LogCapabilityDetectionFailed(ILogger logger, Exception exception);

    private async Task DetectHardwareAcceleratorsAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFFmpegCommandAsync(ffmpegPath, "-hwaccels", cancellationToken);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Skip header line "Hardware acceleration methods:"
        foreach (var line in lines.Skip(1))
        {
            var hwAccel = line.ToLowerInvariant() switch
            {
                "vaapi" => HardwareAccelerationKind.Vaapi,
                "qsv" => HardwareAccelerationKind.Qsv,
                "cuda" or "nvdec" => HardwareAccelerationKind.Nvenc,
                "dxva2" or "d3d11va" => HardwareAccelerationKind.Amf,
                "videotoolbox" => HardwareAccelerationKind.VideoToolbox,
                "rkmpp" => HardwareAccelerationKind.Rkmpp,
                "v4l2m2m" => HardwareAccelerationKind.V4L2M2M,
                _ => (HardwareAccelerationKind?)null
            };

            if (hwAccel is { } accel && accel != HardwareAccelerationKind.None)
            {
                this.supportedHwAccel.Add(accel);
            }
        }
    }

    private async Task DetectEncodersAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFFmpegCommandAsync(ffmpegPath, "-encoders", cancellationToken);

        // Parse encoder lines: " V....D h264_videotoolbox    VideoToolbox H.264 Encoder"
        // Format: <6 flags> <name> <description>
        // Flags: V/A/S, F, S, X, B, D (or '.' for each position)
        var encoderRegex = new Regex(@"^\s*[VAS\.][\w\.]{5}\s+(\S+)\s+", RegexOptions.Multiline);
        var matches = encoderRegex.Matches(output);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                this.supportedEncoders.Add(match.Groups[1].Value);
            }
        }
    }

    private async Task DetectDecodersAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFFmpegCommandAsync(ffmpegPath, "-decoders", cancellationToken);

        // Parse decoder lines: " V....D h264_cuvid        Nvidia CUVID H.264 decoder"
        // Format: <6 flags> <name> <description>
        // Flags: V/A/S, F, S, X, B, D (or '.' for each position)
        var decoderRegex = new Regex(@"^\s*[VAS\.][\w\.]{5}\s+(\S+)\s+", RegexOptions.Multiline);
        var matches = decoderRegex.Matches(output);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                this.supportedDecoders.Add(match.Groups[1].Value);
            }
        }
    }

    private async Task DetectFiltersAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        var output = await RunFFmpegCommandAsync(ffmpegPath, "-filters", cancellationToken);

        // Parse filter lines: " ... scale_cuda        V->V       Scale CUDA video"
        // Format: <flags> <name> <type> <description>
        var filterRegex = new Regex(@"^\s*[T\.]{3}\s+(\S+)\s+", RegexOptions.Multiline);
        var matches = filterRegex.Matches(output);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                this.supportedFilters.Add(match.Groups[1].Value);
            }
        }
    }

    private void DetermineRecommendedAcceleration()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Prefer QSV > NVENC > AMF > None
            if (this.SupportsHwAccel(HardwareAccelerationKind.Qsv) &&
                this.SupportsEncoder("h264_qsv"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Qsv;
            }
            else if (this.SupportsHwAccel(HardwareAccelerationKind.Nvenc) &&
                     this.SupportsEncoder("h264_nvenc"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Nvenc;
            }
            else if (this.SupportsHwAccel(HardwareAccelerationKind.Amf) &&
                     this.SupportsEncoder("h264_amf"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Amf;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: Prefer VAAPI > NVENC > RKMPP > V4L2M2M > None
            if (this.SupportsHwAccel(HardwareAccelerationKind.Vaapi) &&
                this.SupportsEncoder("h264_vaapi"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Vaapi;
            }
            else if (this.SupportsHwAccel(HardwareAccelerationKind.Nvenc) &&
                     this.SupportsEncoder("h264_nvenc"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Nvenc;
            }
            else if (this.SupportsHwAccel(HardwareAccelerationKind.Rkmpp) &&
                     this.SupportsEncoder("h264_rkmpp"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.Rkmpp;
            }
            else if (this.SupportsHwAccel(HardwareAccelerationKind.V4L2M2M) &&
                     this.SupportsEncoder("h264_v4l2m2m"))
            {
                this.recommendedAcceleration = HardwareAccelerationKind.V4L2M2M;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && (this.SupportsHwAccel(HardwareAccelerationKind.VideoToolbox) && this.SupportsEncoder("h264_videotoolbox")))
        {
            this.recommendedAcceleration = HardwareAccelerationKind.VideoToolbox;
        }
    }

    private void UpdateTranscodeOptions()
    {
        var options = this.transcodeOptions.CurrentValue;

        options.DetectedCapabilities.Clear();
        foreach (var hwAccel in this.supportedHwAccel)
        {
            options.DetectedCapabilities[hwAccel.ToString()] = true;
        }

        options.AvailableEncoders.Clear();
        options.AvailableEncoders.AddRange(this.supportedEncoders);

        options.AvailableFilters.Clear();
        options.AvailableFilters.AddRange(this.supportedFilters);

        options.RecommendedAcceleration = this.recommendedAcceleration;
    }
}
