// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for starting a full library scan.
/// </summary>
public sealed class StartLibraryScanInput
{
    /// <summary>
    /// Gets or sets the library section identifier.
    /// </summary>
    [ID]
    public Guid LibrarySectionId { get; set; }
}
