// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Overrides color properties to ensure consistent output (Rec. 709 for SDR).
/// Applied to all videos as the first filter in the chain.
/// </summary>
public sealed class ColorPropertiesFilter : VideoFilterBase
{
    /// <inheritdoc/>
    public override string Name => "ColorProperties";

    /// <inheritdoc/>
    public override int Order => FilterOrder.ColorProperties;

    /// <inheritdoc/>
    public override bool Supports(VideoFilterContext context)
    {
        // Always apply to set consistent color space
        return true;
    }

    /// <inheritdoc/>
    public override IEnumerable<string> Build(VideoFilterContext context)
    {
        // Set color properties to Rec. 709 (standard for HD SDR content)
        yield return "setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709";
    }
}
