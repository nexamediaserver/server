// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Handles video rotation/transpose using platform-appropriate filters.
/// </summary>
public sealed class TransposeFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "Transpose";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Transpose;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Only apply if rotation is needed
        return context.Rotation != 0 && context.Rotation % 90 == 0;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        // Convert rotation to transpose direction
        // 0 = 90° clockwise, 1 = 90° counter-clockwise, 2 = 180°
        var direction = context.Rotation switch
        {
            90 => 1,   // 90° clockwise
            180 => 2,  // 180°
            270 => 0,  // 270° = 90° counter-clockwise
            _ => -1
        };

        if (direction == -1)
        {
            yield break; // Invalid rotation
        }

        var filter = context.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Qsv when context.Capabilities.SupportsFilter("vpp_qsv") =>
                $"vpp_qsv=transpose={direction}",

            HardwareAccelerationKind.Vaapi when context.Capabilities.SupportsFilter("transpose_vaapi") =>
                $"transpose_vaapi=dir={direction}",

            HardwareAccelerationKind.Nvenc when context.Capabilities.SupportsFilter("transpose_cuda") =>
                $"transpose_cuda=dir={direction}",

            HardwareAccelerationKind.Amf when context.Capabilities.SupportsFilter("transpose_opencl") =>
                $"transpose_opencl=dir={direction}",

            // Software fallback
            _ => direction == 2
                ? "transpose=1,transpose=1" // 180° = two 90° rotations
                : $"transpose={direction}"
        };

        yield return filter;
    }
}
