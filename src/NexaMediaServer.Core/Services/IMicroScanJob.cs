// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Processes coalesced filesystem change events by performing targeted mini-scans.
/// </summary>
public interface IMicroScanJob
{
    /// <summary>
    /// Executes a micro-scan for the specified coalesced change event.
    /// </summary>
    /// <param name="changeEvent">The coalesced change event containing paths to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(
        CoalescedChangeEvent changeEvent,
        CancellationToken cancellationToken = default
    );
}
