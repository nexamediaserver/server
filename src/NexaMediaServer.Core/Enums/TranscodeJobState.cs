// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Represents the state of a transcoding job.
/// </summary>
public enum TranscodeJobState
{
    /// <summary>
    /// Job is queued and waiting to start.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently transcoding.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job was cancelled by user or session expiry.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Job failed due to an error.
    /// </summary>
    Failed = 4,
}
