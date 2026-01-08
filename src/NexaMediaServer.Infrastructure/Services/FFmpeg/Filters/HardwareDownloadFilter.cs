// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Downloads hardware frames to software when using HW decoder with SW encoder.
/// Applied automatically when hardware decoder is used with software encoder.
/// </summary>
public sealed class HardwareDownloadFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "HardwareDownload";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Format + 1; // After format conversion, before subtitle overlay

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Download needed when: HW decoder → SW encoder
        return context.IsHardwareDecoder &&
               !context.IsHardwareEncoder &&
               context.HardwareAcceleration != HardwareAccelerationKind.None;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        // hwdownload transfers frames from GPU to system memory
        // format=nv12 ensures a common pixel format for software encoding
        yield return "hwdownload";
        yield return "format=nv12";
    }
}
