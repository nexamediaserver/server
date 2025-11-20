// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the status of a library scan operation.
/// </summary>
public enum LibraryScanStatus
{
    /// <summary>
    /// The library scan is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The library scan is running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The library scan is completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The library scan failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The library scan was cancelled.
    /// </summary>
    Cancelled = 4,
}
