// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing a condition evaluated against media attributes.
/// </summary>
public sealed class ProfileConditionInput
{
    /// <summary>
    /// Gets or sets the comparison operator (e.g., Equals, Contains, IsIn, NotEquals).
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name to evaluate.
    /// </summary>
    public string Property { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the condition is mandatory.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the condition applies only when transcoding.
    /// </summary>
    public bool IsRequiredForTranscoding { get; set; }

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="ProfileCondition"/> built from this input.</returns>
    internal ProfileCondition ToDto()
    {
        return new ProfileCondition
        {
            Condition = this.Condition,
            Property = this.Property,
            Value = this.Value ?? string.Empty,
            IsRequired = this.IsRequired,
            IsRequiredForTranscoding = this.IsRequiredForTranscoding,
        };
    }
}
