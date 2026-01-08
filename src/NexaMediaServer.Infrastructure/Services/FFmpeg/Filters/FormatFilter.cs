// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Converts pixel format to ensure encoder compatibility.
/// </summary>
public sealed class FormatFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "Format";

    /// <inheritdoc/>
    public override int Order => FilterOrder.Format;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Apply format conversion for software path or when not using HW frames
        return IsSoftwarePath(context) || !context.IsHardwareEncoder;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        // Choose format based on target codec and HDR status
        var format = context.IsHdr ? "yuv420p10le" : "yuv420p";

        yield return $"format={format}";
    }
}
