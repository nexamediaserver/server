// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Represents a video filter that can be applied in the FFmpeg filter pipeline.
/// </summary>
public interface IVideoFilter
{
    /// <summary>
    /// Gets the name of this filter for logging and debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the execution order. Lower values execute first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Determines whether this filter should be applied in the given context.
    /// </summary>
    /// <param name="context">The filter context containing codec info and capabilities.</param>
    /// <returns>True if the filter should be applied; otherwise, false.</returns>
    bool Supports(VideoFilterContext context);

    /// <summary>
    /// Builds the FFmpeg filter string(s) for this filter.
    /// </summary>
    /// <param name="context">The filter context containing codec info and capabilities.</param>
    /// <returns>The filter strings to add to the filter chain.</returns>
    IEnumerable<string> Build(VideoFilterContext context);
}
