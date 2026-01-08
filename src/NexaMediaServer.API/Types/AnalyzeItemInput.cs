// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input for triggering file analysis, GoP-index generation, and trickplay generation for a metadata item.
/// </summary>
public sealed class AnalyzeItemInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier.
    /// </summary>
    [ID]
    public Guid ItemId { get; set; }
}
