// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the type of filesystem change.
/// </summary>
public enum FileSystemChangeType
{
    /// <summary>
    /// A file or directory was created.
    /// </summary>
    Created,

    /// <summary>
    /// A file or directory was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file or directory was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file or directory was renamed.
    /// </summary>
    Renamed,
}
