// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// The result of evaluating profile conditions.
/// </summary>
public sealed class ProfileEvaluationResult
{
    /// <summary>
    /// Gets a successful result with no failures.
    /// </summary>
    public static ProfileEvaluationResult Success { get; } = new() { Passed = true };

    /// <summary>
    /// Gets a value indicating whether all conditions passed.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Gets the reasons why evaluation failed, if any.
    /// </summary>
    public TranscodeReason FailedReasons { get; init; }

    /// <summary>
    /// Gets the specific failed conditions for diagnostic purposes.
    /// </summary>
    public IReadOnlyList<string> FailedConditions { get; init; } = [];

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="reasons">The transcode reasons.</param>
    /// <param name="failedConditions">Human-readable failed condition descriptions.</param>
    /// <returns>A failure result.</returns>
    public static ProfileEvaluationResult Failure(
        TranscodeReason reasons,
        IReadOnlyList<string> failedConditions) =>
        new()
        {
            Passed = false,
            FailedReasons = reasons,
            FailedConditions = failedConditions,
        };
}
