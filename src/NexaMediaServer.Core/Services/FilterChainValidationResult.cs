// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Represents the result of filter chain validation.
/// </summary>
public sealed record FilterChainValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the filter chain is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors, if any.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Gets a value indicating whether hardware pipeline should be disabled
    /// to resolve validation issues.
    /// </summary>
    public bool RequiresSoftwareFallback { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid result with no errors.</returns>
    public static FilterChainValidationResult Success() => new()
    {
        IsValid = true,
        Errors = Array.Empty<string>(),
        RequiresSoftwareFallback = false
    };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation error messages.</param>
    /// <param name="requiresSoftwareFallback">Whether software fallback is recommended.</param>
    /// <returns>An invalid result with the specified errors.</returns>
    public static FilterChainValidationResult Failure(
        IReadOnlyList<string> errors,
        bool requiresSoftwareFallback = false) => new()
        {
            IsValid = false,
            Errors = errors,
            RequiresSoftwareFallback = requiresSoftwareFallback
        };
}
