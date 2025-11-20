// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing subtitle delivery capabilities.
/// </summary>
public sealed class SubtitleProfileInput
{
    /// <summary>
    /// Gets or sets the subtitle format.
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery method (External/Embed/Encode).
    /// </summary>
    public string Method { get; set; } = "External";

    /// <summary>
    /// Gets or sets the delivery protocol when applicable (e.g., hls/dash).
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets languages this profile covers, if constrained.
    /// </summary>
    public List<string> Languages { get; set; } = [];

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="SubtitleProfile"/> built from this input.</returns>
    internal SubtitleProfile ToDto()
    {
        return new SubtitleProfile
        {
            Format = this.Format,
            Method = this.Method,
            Protocol = this.Protocol,
            Languages = this.Languages,
        };
    }
}
