// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Builds and executes the video filter pipeline.
/// </summary>
public sealed class VideoFilterPipeline
{
    private readonly IEnumerable<IVideoFilter> filters;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFilterPipeline"/> class.
    /// </summary>
    /// <param name="filters">All available video filters.</param>
    public VideoFilterPipeline(IEnumerable<IVideoFilter> filters)
    {
        this.filters = filters;
    }

    /// <summary>
    /// Builds the complete filter chain for the given context.
    /// </summary>
    /// <param name="context">The filter context.</param>
    /// <returns>The filter chain as a single string, or null if no filters are needed.</returns>
    public string? Build(VideoFilterContext context)
    {
        var applicableFilters = this.filters
            .Where(f => f.Supports(context))
            .OrderBy(f => f.Order)
            .ToList();

        if (applicableFilters.Count == 0)
        {
            return null;
        }

        var filterStrings = new List<string>();

        foreach (var filter in applicableFilters)
        {
            var filterOutput = filter.Build(context);
            filterStrings.AddRange(filterOutput);
        }

        return filterStrings.Count > 0
            ? string.Join(",", filterStrings)
            : null;
    }
}
