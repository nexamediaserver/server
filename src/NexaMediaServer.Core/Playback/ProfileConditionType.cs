// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// Comparison types for profile conditions.
/// </summary>
public static class ProfileConditionType
{
    /// <summary>
    /// Value must equal the condition value.
    /// </summary>
    public const string Equal = "Equals";

    /// <summary>
    /// Value must not equal the condition value.
    /// </summary>
    public const string NotEqual = "NotEquals";

    /// <summary>
    /// Value must be less than or equal to the condition value.
    /// </summary>
    public const string LessThanEqual = "LessThanEqual";

    /// <summary>
    /// Value must be greater than or equal to the condition value.
    /// </summary>
    public const string GreaterThanEqual = "GreaterThanEqual";

    /// <summary>
    /// Value must be in the comma-separated list.
    /// </summary>
    public const string EqualsAny = "EqualsAny";

    /// <summary>
    /// Value must match the regex pattern.
    /// </summary>
    public const string Matches = "Matches";
}
