// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Specifies the final action to take for a path after event coalescing.
/// </summary>
internal enum PathFinalAction
{
    /// <summary>
    /// No action needed (e.g., created then deleted).
    /// </summary>
    Ignore,

    /// <summary>
    /// Path needs to be scanned for changes.
    /// </summary>
    Scan,

    /// <summary>
    /// Path needs to be removed from the database.
    /// </summary>
    Remove,
}
