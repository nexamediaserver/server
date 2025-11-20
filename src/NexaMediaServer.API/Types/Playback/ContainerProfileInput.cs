// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing container-specific constraints.
/// </summary>
public sealed class ContainerProfileInput
{
    /// <summary>
    /// Gets or sets the media type (Video/Audio/Photo).
    /// </summary>
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the conditions that gate support.
    /// </summary>
    public List<ProfileConditionInput> Conditions { get; set; } = [];

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="ContainerProfile"/> built from this input.</returns>
    internal ContainerProfile ToDto()
    {
        return new ContainerProfile
        {
            Type = this.Type,
            Conditions = this.Conditions.Select(c => c.ToDto()).ToList(),
        };
    }
}
