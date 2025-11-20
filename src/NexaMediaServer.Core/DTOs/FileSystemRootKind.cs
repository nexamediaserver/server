// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Categorizes filesystem root types.
/// </summary>
public enum FileSystemRootKind
{
    /// <summary>
    /// The primary OS root (e.g., / or C:\).
    /// </summary>
    Root,

    /// <summary>
    /// A logical drive (Windows) or volume.
    /// </summary>
    Drive,

    /// <summary>
    /// A mounted filesystem.
    /// </summary>
    Mount,
}
