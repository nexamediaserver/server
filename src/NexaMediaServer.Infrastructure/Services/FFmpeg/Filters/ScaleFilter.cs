// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Scales video to target resolution using platform-appropriate filters.
/// </summary>
public sealed class ScaleFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "Scale";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Scale;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        return NeedsScaling(context);
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        var width = context.TargetWidth!.Value;
        var height = context.TargetHeight!.Value;

        var filter = context.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("vpp_qsv") =>
                $"vpp_qsv=w={width}:h={height}:format=nv12:mode=hq",

            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("scale_qsv") =>
                $"scale_qsv=w={width}:h={height}:format=nv12:mode=hq",

            HardwareAccelerationKind.Vaapi when context.Capabilities.SupportsFilter("scale_vaapi") =>
                $"scale_vaapi=w={width}:h={height}:format=nv12:mode=hq",

            HardwareAccelerationKind.Nvenc when context.Capabilities.SupportsFilter("scale_cuda") =>
                $"scale_cuda=w={width}:h={height}:format=yuv420p",

            HardwareAccelerationKind.Amf when context.Capabilities.SupportsFilter("scale_opencl") =>
                $"scale_opencl=w={width}:h={height}:format=nv12",

            HardwareAccelerationKind.VideoToolbox when context.Capabilities.SupportsFilter("scale_vt") =>
                $"scale_vt=w={width}:h={height}",

            // Software fallback with zscale (better quality) or scale
            _ when context.Capabilities.SupportsFilter("zscale") =>
                $"zscale=w={width}:h={height}:filter=lanczos",

            _ => $"scale=w={width}:h={height}:flags=lanczos"
        };

        yield return filter;
    }
}
