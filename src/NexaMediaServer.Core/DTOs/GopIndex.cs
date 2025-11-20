// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a parsed GoP index for a video stream.
/// </summary>
public sealed class GopIndex
{
    /// <summary>
    /// Gets or sets the timebase numerator. Defaults to 1.
    /// </summary>
    public int TimebaseNumerator { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timebase denominator. Defaults to 1000 (milliseconds).
    /// </summary>
    public int TimebaseDenominator { get; set; } = 1000;

    /// <summary>
    /// Gets the groups in chronological order.
    /// </summary>
    public List<GopGroup> Groups { get; } = new();
}
