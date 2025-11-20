// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the various types of optical discs.
/// </summary>
public enum DiscType
{
    /// <summary>
    /// A DVD Video disc.
    /// </summary>
    DVD,

    /// <summary>
    /// A Blu-ray video disc.
    /// </summary>
    BluRay,

    /// <summary>
    /// A Ultra HD Blu-ray video disc.
    /// </summary>
    UHD,

    /// <summary>
    /// An Audio CD disc.
    /// </summary>
    AudioCD,

    /// <summary>
    /// A Photo CD disc.
    /// </summary>
    PhotoCD,

    /// <summary>
    /// A data disc, generally used for games or other non-media content.
    /// </summary>
    DataDisc,
}
