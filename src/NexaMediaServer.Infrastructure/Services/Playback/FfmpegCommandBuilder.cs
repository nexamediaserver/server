// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FFMpegCore;
using FFMpegCore.Pipes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.FFmpeg;
using NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Default implementation of FFmpeg command execution helpers for playback.
/// </summary>
public sealed partial class FfmpegCommandBuilder : IFfmpegCommandBuilder
{
    private readonly ILogger<FfmpegCommandBuilder> logger;
    private readonly IFfmpegCapabilities capabilities;
    private readonly HardwareAccelerationHelper hwAccelHelper;
    private readonly IOptionsMonitor<TranscodeOptions> transcodeOptions;
    private readonly VideoFilterPipeline filterPipeline;
    private readonly IFilterChainValidator filterChainValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegCommandBuilder"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="capabilities">The FFmpeg capabilities.</param>
    /// <param name="transcodeOptions">The transcode options.</param>
    /// <param name="filterPipeline">The video filter pipeline.</param>
    /// <param name="filterChainValidator">The filter chain validator.</param>
    public FfmpegCommandBuilder(
        ILogger<FfmpegCommandBuilder> logger,
        IFfmpegCapabilities capabilities,
        IOptionsMonitor<TranscodeOptions> transcodeOptions,
        VideoFilterPipeline filterPipeline,
        IFilterChainValidator filterChainValidator)
    {
        this.logger = logger;
        this.capabilities = capabilities;
        this.hwAccelHelper = new HardwareAccelerationHelper(capabilities);
        this.transcodeOptions = transcodeOptions;
        this.filterPipeline = filterPipeline;
        this.filterChainValidator = filterChainValidator;
    }

    /// <inheritdoc />
    public async Task RemuxToStreamAsync(
        string inputPath,
        string targetContainer,
        Stream output,
        HardwareAccelerationKind hardwareAcceleration,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sink = new StreamPipeSink(output);
        var processor = FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToPipe(
                sink,
                options =>
                {
                    options.WithVideoCodec("copy");
                    options.WithAudioCodec("copy");
                    options.WithCustomArgument("-movflags +faststart");
                    options.WithCustomArgument($"-f {targetContainer}");
                }
            );

        // Hardware acceleration flags are intentionally omitted for now; pluggable hooks exist via options when supported by FFMpegCore.
        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task RemuxToStreamWithSeekAsync(
        string inputPath,
        string targetContainer,
        Stream output,
        long seekMs,
        HardwareAccelerationKind hardwareAcceleration,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Convert milliseconds to TimeSpan for FFMpegCore
        var seekTime = TimeSpan.FromMilliseconds(seekMs);

        var sink = new StreamPipeSink(output);
        var processor = FFMpegArguments
            .FromFileInput(
                inputPath,
                verifyExists: true,
                options =>
                {
                    // Use -ss before input for fast keyframe-based seeking
                    options.Seek(seekTime);
                }
            )
            .OutputToPipe(
                sink,
                options =>
                {
                    options.WithVideoCodec("copy");
                    options.WithAudioCodec("copy");
                    options.WithCustomArgument("-movflags +frag_keyframe+empty_moov");
                    options.WithCustomArgument($"-f {targetContainer}");
                }
            );

        // Hardware acceleration flags are intentionally omitted for now; pluggable hooks exist via options when supported by FFMpegCore.
        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task CreateDashAsync(DashTranscodeJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // FFmpeg's DASH muxer creates subdirectories for each adaptation set (e.g., 0/, 1/)
        // Pre-create these directories to avoid "No such file or directory" errors
        EnsureDashSubdirectories(job);

        // Determine if hardware encoding will actually be used
        var hwAccel = job.UseHardwareAcceleration
            ? this.transcodeOptions.CurrentValue.EffectiveAcceleration
            : HardwareAccelerationKind.None;
        var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
        var isHwEncoder = !job.CopyVideo && this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);

        LogTranscodeJobStarted(this.logger, job.InputPath, job.ManifestPath, isHwEncoder);

        var processor = FFMpegArguments
            .FromFileInput(job.InputPath)
            .OutputToFile(
                job.ManifestPath,
                overwrite: true,
                options => this.ConfigureDashOutput(job, options)
            );

        LogFfmpegCommand(this.logger, processor.Arguments);

        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task CreateDashWithSeekAsync(
        DashTranscodeJob job,
        long seekMs,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // FFmpeg's DASH muxer creates subdirectories for each adaptation set (e.g., 0/, 1/)
        // Pre-create these directories to avoid "No such file or directory" errors
        EnsureDashSubdirectories(job);

        // Determine if hardware encoding will actually be used
        var hwAccel = job.UseHardwareAcceleration
            ? this.transcodeOptions.CurrentValue.EffectiveAcceleration
            : HardwareAccelerationKind.None;
        var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
        var isHwEncoder = !job.CopyVideo && this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);

        LogTranscodeJobStarted(this.logger, job.InputPath, job.ManifestPath, isHwEncoder);

        // Use input seeking (-ss before -i) for fast keyframe-based seeking
        var seekSeconds = seekMs / 1000.0;

        var processor = FFMpegArguments
            .FromFileInput(
                job.InputPath,
                verifyExists: true,
                options =>
                {
                    options.Seek(TimeSpan.FromSeconds(seekSeconds));
                }
            )
            .OutputToFile(
                job.ManifestPath,
                overwrite: true,
                options =>
                {
                    // Shift output timestamps to match the seek position
                    // This ensures segments have the correct presentation time for DASH
                    // Without this, segments would have PTS starting at 0 instead of seekSeconds
                    options.WithCustomArgument($"-output_ts_offset {seekSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
                    this.ConfigureDashOutput(job, options);
                }
            );

        LogFfmpegCommand(this.logger, processor.Arguments);

        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task CreateHlsAsync(HlsTranscodeJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure output directory exists
        if (!string.IsNullOrEmpty(job.OutputDirectory))
        {
            Directory.CreateDirectory(job.OutputDirectory);
        }

        var playlistPath = Path.Combine(job.OutputDirectory, $"{job.VariantId}.m3u8");

        // Determine if hardware encoding will actually be used
        var hwAccel = job.UseHardwareAcceleration
            ? job.HardwareAcceleration
            : HardwareAccelerationKind.None;
        var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
        var isHwEncoder = !job.CopyVideo && this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);

        LogTranscodeJobStarted(this.logger, job.InputPath, playlistPath, isHwEncoder);

        var processor = FFMpegArguments
            .FromFileInput(job.InputPath)
            .OutputToFile(
                playlistPath,
                overwrite: true,
                options => this.ConfigureHlsOutput(job, options)
            );

        LogFfmpegCommand(this.logger, processor.Arguments);

        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task CreateHlsWithSeekAsync(
        HlsTranscodeJob job,
        long seekMs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure output directory exists
        if (!string.IsNullOrEmpty(job.OutputDirectory))
        {
            Directory.CreateDirectory(job.OutputDirectory);
        }

        var playlistPath = Path.Combine(job.OutputDirectory, $"{job.VariantId}.m3u8");
        var seekSeconds = seekMs / 1000.0;

        // Determine if hardware encoding will actually be used
        var hwAccel = job.UseHardwareAcceleration
            ? job.HardwareAcceleration
            : HardwareAccelerationKind.None;
        var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
        var isHwEncoder = !job.CopyVideo && this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);

        LogTranscodeJobStarted(this.logger, job.InputPath, playlistPath, isHwEncoder);

        var processor = FFMpegArguments
            .FromFileInput(
                job.InputPath,
                verifyExists: true,
                options =>
                {
                    options.Seek(TimeSpan.FromSeconds(seekSeconds));
                }
            )
            .OutputToFile(
                playlistPath,
                overwrite: true,
                options => this.ConfigureHlsOutput(job, options)
            );

        LogFfmpegCommand(this.logger, processor.Arguments);

        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Pre-creates subdirectories that FFmpeg's DASH muxer will use for adaptation sets.
    /// </summary>
    /// <param name="job">The DASH transcode job containing output directory information.</param>
    private static void EnsureDashSubdirectories(DashTranscodeJob job)
    {
        // When using -use_template 1, FFmpeg's DASH muxer creates subdirectories
        // named after adaptation set IDs (0/, 1/, etc.)
        // We need to pre-create these directories to avoid "No such file or directory" errors
        var baseDir = Path.GetDirectoryName(job.ManifestPath);
        if (string.IsNullOrEmpty(baseDir))
        {
            return;
        }

        // Create subdirectories for each potential adaptation set
        // Video is typically id=0, audio is id=1 (or id=0 if no video)
        var subdirCount = 0;
        if (job.HasVideo)
        {
            subdirCount++;
        }

        if (job.HasAudio)
        {
            subdirCount++;
        }

        for (var i = 0; i < subdirCount; i++)
        {
            var subdir = Path.Combine(baseDir, i.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(subdir);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Filter chain validation failed, falling back to software pipeline: {Errors}")]
    private static partial void LogFallingBackToSoftwarePipeline(ILogger logger, string errors);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Filter chain validation warnings: {Errors}")]
    private static partial void LogFilterChainValidationWarning(ILogger logger, string errors);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "FFmpeg command: {Command}")]
    private static partial void LogFfmpegCommand(ILogger logger, string command);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "HLS transcode decision - HW Accel: {HwAccel}, UseHwDecoder: {UseHwDecoder}, UseHwEncoder: {UseHwEncoder}, Encoder: {Encoder}, SourceCodec: {SourceCodec}, TargetCodec: {TargetCodec}")]
    private static partial void LogHlsHardwareDecision(
        ILogger logger,
        HardwareAccelerationKind hwAccel,
        bool useHwDecoder,
        bool useHwEncoder,
        string encoder,
        string sourceCodec,
        string targetCodec);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "DASH transcode decision - HW Accel: {HwAccel}, UseHwDecoder: {UseHwDecoder}, UseHwEncoder: {UseHwEncoder}, Encoder: {Encoder}, SourceCodec: {SourceCodec}, TargetCodec: {TargetCodec}")]
    private static partial void LogDashHardwareDecision(
        ILogger logger,
        HardwareAccelerationKind hwAccel,
        bool useHwDecoder,
        bool useHwEncoder,
        string encoder,
        string sourceCodec,
        string targetCodec);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transcode job started - Input: {InputPath}, Output: {OutputPath}, HW Encoding: {HwEncoding}")]
    private static partial void LogTranscodeJobStarted(ILogger logger, string inputPath, string outputPath, bool hwEncoding);

    private void ConfigureDashOutput(DashTranscodeJob job, FFMpegArgumentOptions options)
    {
        // Determine hardware acceleration settings
        var hwAccel = job.UseHardwareAcceleration
            ? this.transcodeOptions.CurrentValue.EffectiveAcceleration
            : HardwareAccelerationKind.None;
        var useHwDecoder = job.UseHardwareDecoder &&
                          hwAccel != HardwareAccelerationKind.None &&
                          this.capabilities.IsHardwareDecoderAvailable(job.SourceVideoCodec, hwAccel);

        // Apply hardware device initialization when any HW stage is active
        var wantsHwDevice = !job.CopyVideo && hwAccel != HardwareAccelerationKind.None;
        if (wantsHwDevice)
        {
            var deviceArgs = HardwareAccelerationHelper.GetHardwareDeviceArgs(hwAccel);
            if (!string.IsNullOrEmpty(deviceArgs))
            {
                options.WithCustomArgument(deviceArgs);
            }

            if (useHwDecoder)
            {
                var decoderArgs = HardwareAccelerationHelper.GetHardwareDecoderArgs(hwAccel);
                if (!string.IsNullOrEmpty(decoderArgs))
                {
                    options.WithCustomArgument(decoderArgs);
                }
            }

            // Ensure filters know which hardware device to target (needed for hwupload)
            var filterDevice = HardwareAccelerationHelper.GetFilterHardwareDeviceArg(hwAccel);
            if (!string.IsNullOrEmpty(filterDevice))
            {
                options.WithCustomArgument(filterDevice);
            }
        }

        // Map streams
        if (job.HasVideo)
        {
            options.WithCustomArgument("-map 0:v?");
        }

        if (job.HasAudio)
        {
            options.WithCustomArgument("-map 0:a?");
        }

        if (!job.HasVideo && !job.HasAudio)
        {
            options.WithCustomArgument("-map 0");
        }

        options.WithCustomArgument("-preset veryfast");
        options.WithCustomArgument("-movflags +faststart");
        options.WithCustomArgument("-use_timeline 1");
        options.WithCustomArgument("-use_template 1");
        options.WithCustomArgument("-hls_playlist 0");
        options.WithCustomArgument("-single_file 0");
        options.WithCustomArgument("-window_size 0");
        options.WithCustomArgument("-extra_window_size 0");
        options.WithCustomArgument("-init_seg_name init-stream$RepresentationID$.m4s");
        options.WithCustomArgument(
            "-media_seg_name chunk-stream$RepresentationID$-$Number%05d$.m4s"
        );
        options.WithCustomArgument($"-seg_duration {Math.Max(1, job.SegmentSeconds)}");

        // For seek-based transcodes, start segment numbering from the calculated index
        // so that segment filenames match what the client expects from the original manifest
        if (job.StartSegmentNumber.HasValue && job.StartSegmentNumber.Value > 1)
        {
            options.WithCustomArgument($"-start_number {job.StartSegmentNumber.Value}");
        }

        if (!string.IsNullOrWhiteSpace(job.ForceKeyFramesExpression))
        {
            options.WithCustomArgument($"-force_key_frames {job.ForceKeyFramesExpression}");
        }

        if (job.HasVideo)
        {
            if (job.CopyVideo)
            {
                options.WithVideoCodec("copy");
            }
            else
            {
                // Select encoder with hardware acceleration support
                var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
                var isHwEncoder = this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);
                var encoder = this.hwAccelHelper.SelectEncoder(targetCodec, hwAccel);

                // Log the hardware decision for debugging
                LogDashHardwareDecision(
                    this.logger,
                    hwAccel,
                    useHwDecoder,
                    isHwEncoder,
                    encoder,
                    job.SourceVideoCodec,
                    targetCodec);

                options.WithVideoCodec(encoder);

                // Build and apply video filter chain
                var filterChain = this.BuildVideoFilterChain(
                    hwAccel,
                    useHwDecoder,
                    isHwEncoder,
                    job.SourceVideoCodec,
                    targetCodec,
                    job.SourceWidth,
                    job.SourceHeight,
                    job.TargetWidth,
                    job.TargetHeight,
                    job.IsInterlaced,
                    job.IsHdr,
                    job.EnableToneMapping,
                    job.Rotation);

                if (!string.IsNullOrEmpty(filterChain))
                {
                    options.WithCustomArgument($"-vf {filterChain}");
                }
            }
        }
        else
        {
            options.WithCustomArgument("-vn");
        }

        if (job.HasAudio)
        {
            if (job.CopyAudio)
            {
                options.WithAudioCodec("copy");
            }
            else
            {
                options.WithAudioCodec(
                    string.IsNullOrWhiteSpace(job.AudioCodec) ? "aac" : job.AudioCodec
                );
            }
        }
        else
        {
            options.WithCustomArgument("-an");
        }

        var adaptationSets = new List<string>();
        if (job.HasVideo)
        {
            adaptationSets.Add("id=0,streams=v");
        }

        if (job.HasAudio)
        {
            var audioSetId = job.HasVideo ? 1 : 0;
            adaptationSets.Add($"id={audioSetId},streams=a");
        }

        if (adaptationSets.Count > 0)
        {
            options.WithCustomArgument(
                $"-adaptation_sets \"{string.Join(' ', adaptationSets)}\""
            );
        }

        options.WithCustomArgument("-strict -2");
        options.WithCustomArgument("-f dash");
    }

    private void ConfigureHlsOutput(HlsTranscodeJob job, FFMpegArgumentOptions options)
    {
        // Determine hardware acceleration settings
        var hwAccel = job.UseHardwareAcceleration
            ? job.HardwareAcceleration
            : HardwareAccelerationKind.None;
        var useHwDecoder = job.UseHardwareDecoder &&
                          hwAccel != HardwareAccelerationKind.None &&
                          this.capabilities.IsHardwareDecoderAvailable(job.SourceVideoCodec, hwAccel);

        // Apply hardware device initialization when any HW stage is active
        var wantsHwDevice = !job.CopyVideo && hwAccel != HardwareAccelerationKind.None;
        if (wantsHwDevice)
        {
            var deviceArgs = HardwareAccelerationHelper.GetHardwareDeviceArgs(hwAccel);
            if (!string.IsNullOrEmpty(deviceArgs))
            {
                options.WithCustomArgument(deviceArgs);
            }

            if (useHwDecoder)
            {
                var decoderArgs = HardwareAccelerationHelper.GetHardwareDecoderArgs(hwAccel);
                if (!string.IsNullOrEmpty(decoderArgs))
                {
                    options.WithCustomArgument(decoderArgs);
                }
            }

            var filterDevice = HardwareAccelerationHelper.GetFilterHardwareDeviceArg(hwAccel);
            if (!string.IsNullOrEmpty(filterDevice))
            {
                options.WithCustomArgument(filterDevice);
            }
        }

        // Map streams
        if (job.HasVideo)
        {
            options.WithCustomArgument("-map 0:v:0");
        }

        if (job.HasAudio)
        {
            var audioMap = job.AudioStreamIndex.HasValue
                ? $"-map 0:a:{job.AudioStreamIndex.Value}"
                : "-map 0:a:0?";
            options.WithCustomArgument(audioMap);
        }

        // Video encoding
        if (job.HasVideo)
        {
            if (job.CopyVideo)
            {
                options.WithVideoCodec("copy");
            }
            else
            {
                // Select encoder with hardware acceleration support
                var targetCodec = string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec;
                var isHwEncoder = this.hwAccelHelper.SupportsHardwareEncoding(targetCodec, hwAccel);
                var encoder = this.hwAccelHelper.SelectEncoder(targetCodec, hwAccel);

                // Log the hardware decision for debugging
                LogHlsHardwareDecision(
                    this.logger,
                    hwAccel,
                    useHwDecoder,
                    isHwEncoder,
                    encoder,
                    job.SourceVideoCodec,
                    targetCodec);

                options.WithVideoCodec(encoder);

                // Apply video encoding settings
                options.WithCustomArgument("-preset veryfast");

                if (job.VideoBitrate.HasValue)
                {
                    options.WithCustomArgument($"-b:v {job.VideoBitrate.Value}");
                    options.WithCustomArgument($"-maxrate {job.VideoBitrate.Value * 1.5:F0}");
                    options.WithCustomArgument($"-bufsize {job.VideoBitrate.Value * 2}");
                }

                // Build and apply video filter chain
                var filterChain = this.BuildVideoFilterChain(
                    hwAccel,
                    useHwDecoder,
                    isHwEncoder,
                    job.SourceVideoCodec,
                    targetCodec,
                    job.SourceWidth,
                    job.SourceHeight,
                    job.TargetWidth,
                    job.TargetHeight,
                    job.IsInterlaced,
                    job.IsHdr,
                    job.EnableToneMapping,
                    job.Rotation);

                if (!string.IsNullOrEmpty(filterChain))
                {
                    options.WithCustomArgument($"-vf {filterChain}");
                }
            }
        }
        else
        {
            options.WithCustomArgument("-vn");
        }

        // Audio encoding
        if (job.HasAudio)
        {
            if (job.CopyAudio)
            {
                options.WithAudioCodec("copy");
            }
            else
            {
                options.WithAudioCodec(
                    string.IsNullOrWhiteSpace(job.AudioCodec) ? "aac" : job.AudioCodec
                );

                if (job.AudioBitrate.HasValue)
                {
                    options.WithCustomArgument($"-b:a {job.AudioBitrate.Value}");
                }

                if (job.AudioChannels.HasValue)
                {
                    options.WithCustomArgument($"-ac {job.AudioChannels.Value}");
                }
            }
        }
        else
        {
            options.WithCustomArgument("-an");
        }

        // Force keyframes for proper segmentation
        if (!string.IsNullOrWhiteSpace(job.ForceKeyFramesExpression))
        {
            options.WithCustomArgument($"-force_key_frames {job.ForceKeyFramesExpression}");
        }
        else if (!job.CopyVideo)
        {
            // Force keyframes at segment boundaries
            options.WithCustomArgument($"-g {job.SegmentSeconds * 30}");
            options.WithCustomArgument($"-keyint_min {job.SegmentSeconds * 30}");
        }

        // HLS-specific settings
        options.WithCustomArgument($"-hls_time {Math.Max(1, job.SegmentSeconds)}");
        options.WithCustomArgument("-hls_list_size 0"); // Keep all segments in playlist
        options.WithCustomArgument("-hls_playlist_type vod");

        // Use fMP4 segments for better compatibility and seeking
        if (job.UseFragmentedMp4)
        {
            options.WithCustomArgument("-hls_segment_type fmp4");
            options.WithCustomArgument($"-hls_segment_filename {job.OutputDirectory}/{job.VariantId}_%05d.m4s");
            options.WithCustomArgument($"-hls_fmp4_init_filename {job.VariantId}_init.mp4");
        }
        else
        {
            options.WithCustomArgument($"-hls_segment_filename {job.OutputDirectory}/{job.VariantId}_%05d.ts");
        }

        options.WithCustomArgument("-f hls");
    }

    /// <summary>
    /// Builds the video filter chain using the filter pipeline with validation and fallback.
    /// </summary>
    private string? BuildVideoFilterChain(
        HardwareAccelerationKind hwAccel,
        bool isHwDecoder,
        bool isHwEncoder,
        string sourceCodec,
        string targetCodec,
        int sourceWidth,
        int sourceHeight,
        int? targetWidth,
        int? targetHeight,
        bool isInterlaced,
        bool isHdr,
        bool enableToneMapping,
        int rotation)
    {
        var context = new VideoFilterContext
        {
            HardwareAcceleration = hwAccel,
            Capabilities = this.capabilities,
            SourceVideoCodec = sourceCodec,
            TargetVideoCodec = targetCodec,
            SourceWidth = sourceWidth,
            SourceHeight = sourceHeight,
            TargetWidth = targetWidth,
            TargetHeight = targetHeight,
            IsInterlaced = isInterlaced,
            IsHdr = isHdr,
            EnableToneMapping = enableToneMapping,
            Rotation = rotation,
            RequiresSubtitleOverlay = false, // Subtitle burn-in deferred to separate task
            SubtitlePath = null,
            IsHardwareDecoder = isHwDecoder,
            IsHardwareEncoder = isHwEncoder
        };

        // Build filter chain
        var filterChain = this.filterPipeline.Build(context);

        // Validate the filter chain
        var validationResult = this.filterChainValidator.Validate(filterChain, context);

        if (!validationResult.IsValid)
        {
            if (validationResult.RequiresSoftwareFallback)
            {
                LogFallingBackToSoftwarePipeline(this.logger, string.Join("; ", validationResult.Errors));

                // Rebuild with software-only pipeline
                var swContext = context with
                {
                    HardwareAcceleration = HardwareAccelerationKind.None,
                    IsHardwareDecoder = false,
                    IsHardwareEncoder = false
                };

                filterChain = this.filterPipeline.Build(swContext);
            }
            else
            {
                // Log warning but proceed with the filter chain
                LogFilterChainValidationWarning(this.logger, string.Join("; ", validationResult.Errors));
            }
        }

        return filterChain;
    }
}