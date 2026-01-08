// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Base class for video filters providing common functionality.
/// </summary>
public abstract class VideoFilterBase : IVideoFilter
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract int Order { get; }

    /// <inheritdoc/>
    public abstract bool Supports(VideoFilterContext context);

    /// <inheritdoc/>
    public abstract IEnumerable<string> Build(VideoFilterContext context);

    /// <summary>
    /// Checks if the hardware acceleration type matches any of the specified kinds.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <param name="kinds">The hardware acceleration kinds to check.</param>
    /// <returns>True if the context's acceleration matches any of the specified kinds.</returns>
    protected static bool IsHardwareAccel(VideoFilterContext context, params HardwareAccelerationKind[] kinds)
    {
        return kinds.Contains(context.HardwareAcceleration);
    }

    /// <summary>
    /// Checks if software path should be used.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <returns>True if software encoding should be used.</returns>
    protected static bool IsSoftwarePath(VideoFilterContext context)
    {
        return context.HardwareAcceleration == HardwareAccelerationKind.None;
    }

    /// <summary>
    /// Checks if scaling is needed.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <returns>True if the video needs to be scaled to a different resolution.</returns>
    protected static bool NeedsScaling(VideoFilterContext context)
    {
        return context.TargetWidth.HasValue && context.TargetHeight.HasValue &&
               (context.SourceWidth != context.TargetWidth.Value ||
                context.SourceHeight != context.TargetHeight.Value);
    }
}
