// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used to start playback for a metadata item.
/// </summary>
public sealed class PlaybackStartInput
{
    /// <summary>
    /// Gets or sets the metadata item identifier.
    /// </summary>
    [ID("Item")]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets an optional originator descriptor.
    /// </summary>
    public string? Originator { get; set; }

    /// <summary>
    /// Gets or sets an optional JSON payload describing playback context.
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client believes is current.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets an optional capability declaration to upsert.
    /// </summary>
    public PlaybackCapabilityInput? Capability { get; set; }
}
