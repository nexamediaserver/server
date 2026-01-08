// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Deinterlaces interlaced video content using platform-appropriate filters.
/// </summary>
public sealed class DeinterlaceFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "Deinterlace";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Deinterlace;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Only apply if source is interlaced
        return context.IsInterlaced;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        var filter = context.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("deinterlace_qsv") =>
                "deinterlace_qsv=mode=2", // mode=2 = one frame per field

            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("vpp_qsv") =>
                "vpp_qsv=deinterlace=2",

            HardwareAccelerationKind.Vaapi when context.Capabilities.SupportsFilter("deinterlace_vaapi") =>
                "deinterlace_vaapi=mode=bob",

            HardwareAccelerationKind.Nvenc when context.Capabilities.SupportsFilter("yadif_cuda") =>
                "yadif_cuda=mode=send_field:parity=auto:deint=interlaced",

            HardwareAccelerationKind.Amf when context.Capabilities.SupportsFilter("bwdif_opencl") =>
                "bwdif_opencl=mode=send_field:parity=auto",

            // Software fallback - use bwdif (better quality) or yadif
            _ when context.Capabilities.SupportsFilter("bwdif") =>
                "bwdif=mode=send_field:parity=auto:deint=interlaced",

            _ => "yadif=mode=send_field:parity=auto:deint=interlaced"
        };

        yield return filter;
    }
}
