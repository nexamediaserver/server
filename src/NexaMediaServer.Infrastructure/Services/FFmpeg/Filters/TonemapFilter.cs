// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Tone maps HDR content to SDR using platform-appropriate filters.
/// </summary>
public sealed class TonemapFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "Tonemap";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Tonemap;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Only apply if source is HDR and tone mapping is enabled
        return context.IsHdr && context.EnableToneMapping;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        // Tone mapping algorithm: hable (default), bt2390, mobius, reinhard
        const string algorithm = "hable";

        var filters = context.HardwareAcceleration switch
        {
            // VAAPI with OpenCL tone mapping
            HardwareAccelerationKind.Vaapi when context.Capabilities.SupportsFilter("tonemap_vaapi") =>
                new[] { $"tonemap_vaapi=format=nv12:matrix=bt709:primaries=bt709:transfer=bt709" },

            HardwareAccelerationKind.Vaapi when context.Capabilities.SupportsFilter("tonemap_opencl") =>
                new[]
                {
                    "hwmap=derive_device=opencl:mode=read",
                    $"tonemap_opencl=t=bt709:tonemap={algorithm}:desat=0:format=nv12",
                    "hwmap=derive_device=vaapi:reverse=1",
                    "format=vaapi"
                },

            // QSV with OpenCL tone mapping
            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("tonemap_opencl") =>
                new[]
                {
                    "hwmap=derive_device=opencl:mode=read",
                    $"tonemap_opencl=t=bt709:tonemap={algorithm}:desat=0:format=nv12",
                    "hwmap=derive_device=qsv:reverse=1",
                    "format=qsv"
                },

            // NVENC with CUDA tone mapping
            HardwareAccelerationKind.Nvenc when context.Capabilities.SupportsFilter("tonemap_cuda") =>
                new[] { $"tonemap_cuda=t=bt709:tonemap={algorithm}:desat=0:format=yuv420p" },

            // AMD with OpenCL tone mapping
            HardwareAccelerationKind.Amf when context.Capabilities.SupportsFilter("tonemap_opencl") =>
                new[]
                {
                    "hwmap=derive_device=opencl:mode=read",
                    $"tonemap_opencl=t=bt709:tonemap={algorithm}:desat=0:format=nv12",
                    "hwmap=mode=read",
                    "format=nv12"
                },

            // Software fallback - use tonemapx (newer) or zscale
            _ when context.Capabilities.SupportsFilter("tonemapx") =>
                new[] { $"tonemapx=tonemap={algorithm}:desat=0:peak=100" },

            _ when context.Capabilities.SupportsFilter("zscale") =>
                new[] { "zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p" },

            _ => new[] { "zscale=t=bt709:m=bt709:r=tv,format=yuv420p" }
        };

        foreach (var filter in filters)
        {
            yield return filter;
        }
    }
}
