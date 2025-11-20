// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing a direct-play capability.
/// </summary>
public sealed class DirectPlayProfileInput
{
    /// <summary>
    /// Gets or sets the media type (Video/Audio/Photo).
    /// </summary>
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the supported container(s).
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported video codec, if constrained.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the supported audio codec, if constrained.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="DirectPlayProfile"/> built from this input.</returns>
    internal DirectPlayProfile ToDto()
    {
        return new DirectPlayProfile
        {
            Type = this.Type,
            Container = this.Container,
            VideoCodec = this.VideoCodec,
            AudioCodec = this.AudioCodec,
        };
    }
}
