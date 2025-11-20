// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of stream for a media item.
/// </summary>
public enum StreamType
{
    /// <summary>
    /// Audio stream.
    /// </summary>
    Audio = 0,

    /// <summary>
    /// Video stream.
    /// </summary>
    Video = 1,

    /// <summary>
    /// Subtitle stream.
    /// </summary>
    Subtitle = 2,

    /// <summary>
    /// Network stream.
    /// </summary>
    Network = 3,
}
