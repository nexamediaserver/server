// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Validates video filter chains for correctness before execution.
/// </summary>
public interface IFilterChainValidator
{
    /// <summary>
    /// Validates the filter chain for the given context.
    /// Checks for hwupload presence before HW filters when using SW decoder,
    /// hwdownload presence before SW filters when using HW decoder,
    /// and consistent hardware device type throughout the chain.
    /// </summary>
    /// <param name="filterChain">The filter chain string to validate.</param>
    /// <param name="context">The filter context containing decoder/encoder info.</param>
    /// <returns>The validation result with any errors found.</returns>
    FilterChainValidationResult Validate(string? filterChain, VideoFilterContext context);
}
