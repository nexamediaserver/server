// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input mapping response overrides for specific media/container pairs.
/// </summary>
public sealed class ResponseProfileInput
{
    /// <summary>
    /// Gets or sets the media type (Video/Audio/Photo).
    /// </summary>
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the container restriction.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type to advertise.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="ResponseProfile"/> built from this input.</returns>
    internal ResponseProfile ToDto()
    {
        return new ResponseProfile
        {
            Type = this.Type,
            Container = this.Container,
            MimeType = this.MimeType,
        };
    }
}
