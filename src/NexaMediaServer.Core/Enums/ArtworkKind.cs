// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of artwork stored for a metadata item.
/// </summary>
public enum ArtworkKind
{
    /// <summary>
    /// Primary portrait-oriented artwork (often referred to as a cover or poster).
    /// </summary>
    Poster = 0,

    /// <summary>
    /// Landscape-oriented background artwork.
    /// </summary>
    Backdrop = 1,

    /// <summary>
    /// Transparent or stylized logo artwork.
    /// </summary>
    Logo = 2,
}
