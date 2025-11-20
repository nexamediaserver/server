// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing codec-level constraints.
/// </summary>
public sealed class CodecProfileInput
{
    /// <summary>
    /// Gets or sets the media type (Video/Audio/Photo).
    /// </summary>
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the codec this profile applies to, if limited.
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the container restriction, if any.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the conditions that must be satisfied.
    /// </summary>
    public List<ProfileConditionInput> Conditions { get; set; } = [];

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="CodecProfile"/> built from this input.</returns>
    internal CodecProfile ToDto()
    {
        return new CodecProfile
        {
            Type = this.Type,
            Codec = this.Codec,
            Container = this.Container,
            Conditions = this.Conditions.Select(c => c.ToDto()).ToList(),
        };
    }
}
