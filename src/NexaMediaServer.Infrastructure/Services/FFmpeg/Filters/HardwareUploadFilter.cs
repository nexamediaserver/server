// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Uploads software frames to hardware surfaces when using HW acceleration.
/// Applied automatically when software decoder is used with hardware encoder.
/// </summary>
public sealed class HardwareUploadFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "HardwareUpload";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Scale - 1; // Before scaling

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Upload needed when: SW decoder → HW encoder
        return !context.IsHardwareDecoder &&
               context.IsHardwareEncoder &&
               context.HardwareAcceleration != HardwareAccelerationKind.None;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        var filter = context.HardwareAcceleration switch
        {
            HardwareAccelerationKind.Qsv =>
                "hwupload=derive_device=qsv",

            HardwareAccelerationKind.Vaapi =>
                "hwupload=derive_device=vaapi",

            HardwareAccelerationKind.Nvenc =>
                "hwupload=derive_device=cuda",

            HardwareAccelerationKind.Amf =>
                "hwupload=derive_device=d3d11va",

            HardwareAccelerationKind.VideoToolbox =>
                "hwupload=derive_device=videotoolbox",

            _ => null
        };

        if (filter != null)
        {
            yield return filter;
        }
    }
}
