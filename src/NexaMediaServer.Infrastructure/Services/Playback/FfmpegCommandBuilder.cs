// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Pipes;
using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Default implementation of FFmpeg command execution helpers for playback.
/// </summary>
public sealed class FfmpegCommandBuilder : IFfmpegCommandBuilder
{
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

        var processor = FFMpegArguments
            .FromFileInput(job.InputPath)
            .OutputToFile(
                job.ManifestPath,
                overwrite: true,
                options => ConfigureDashOutput(job, options)
            );

        // Hardware acceleration flags are intentionally omitted for now; pluggable hooks exist via options when supported by FFMpegCore.
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
                options => ConfigureDashOutput(job, options)
            );

        await processor.ProcessAsynchronously(true).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void ConfigureDashOutput(DashTranscodeJob job, FFMpegArgumentOptions options)
    {
        options.WithCustomArgument("-map 0");
        options.WithCustomArgument("-preset veryfast");
        options.WithCustomArgument("-movflags +faststart");
        options.WithCustomArgument("-use_timeline 1");
        options.WithCustomArgument("-use_template 1");
        options.WithCustomArgument("-init_seg_name init-stream$RepresentationID$.m4s");
        options.WithCustomArgument(
            "-media_seg_name chunk-stream$RepresentationID$-$Number%05d$.m4s"
        );
        options.WithCustomArgument($"-seg_duration {Math.Max(1, job.SegmentSeconds)}");

        if (!string.IsNullOrWhiteSpace(job.ForceKeyFramesExpression))
        {
            options.WithCustomArgument($"-force_key_frames {job.ForceKeyFramesExpression}");
        }

        if (job.CopyVideo)
        {
            options.WithVideoCodec("copy");
        }
        else
        {
            options.WithVideoCodec(
                string.IsNullOrWhiteSpace(job.VideoCodec) ? "h264" : job.VideoCodec
            );
            if (job.EnableToneMapping)
            {
                options.WithCustomArgument("-vf zscale=t=bt709");
            }
        }

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

        options.WithCustomArgument("-adaptation_sets \"id=0,streams=v id=1,streams=a\"");
        options.WithCustomArgument("-strict -2");
        options.WithCustomArgument("-f dash");
    }
}
