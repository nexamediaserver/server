// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Filter execution order constants.
/// </summary>
public static class FilterOrder
{
    /// <summary>
    /// Color properties override (must be first).
    /// </summary>
    public const int ColorProperties = 5;

    /// <summary>
    /// Deinterlacing.
    /// </summary>
    public const int Deinterlace = 10;

    /// <summary>
    /// Rotation/transpose.
    /// </summary>
    public const int Transpose = 20;

    /// <summary>
    /// Scaling/resizing.
    /// </summary>
    public const int Scale = 30;

    /// <summary>
    /// Tone mapping (HDR to SDR).
    /// </summary>
    public const int Tonemap = 40;

    /// <summary>
    /// Format conversion.
    /// </summary>
    public const int Format = 45;

    /// <summary>
    /// Subtitle overlay (must be last).
    /// </summary>
    public const int SubtitleOverlay = 50;
}
