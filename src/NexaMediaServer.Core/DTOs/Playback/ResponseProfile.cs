// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Maps response behaviors for specific media or container combinations.
/// </summary>
public sealed class ResponseProfile
{
    /// <summary>
    /// Gets or sets the type of media the response applies to.
    /// </summary>
    [Required]
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the container restriction, if any.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the MIME type to respond with.
    /// </summary>
    public string? MimeType { get; set; }
}
