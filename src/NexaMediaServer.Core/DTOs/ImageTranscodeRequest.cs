// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a request to obtain a (possibly transcoded) image variant.
/// </summary>
public sealed class ImageTranscodeRequest
{
    /// <summary>
    /// Gets or sets the source image URI (metadata:// or media:// etc.).
    /// </summary>
    public string SourceUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the desired width in pixels (optional).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the desired height in pixels (optional).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the output format container/extension (e.g. jpg, png, webp, avif).
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets an optional quality hint (0-100) for lossy formats.
    /// </summary>
    public int? Quality { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether aspect ratio should be preserved (default true).
    /// </summary>
    public bool PreserveAspectRatio { get; set; } = true;
}
