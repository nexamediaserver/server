// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Request to start playback for a given metadata item.
/// </summary>
public sealed class PlaybackStartRequest
{
    /// <summary>
    /// Gets or sets the database identifier of the metadata item to play.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets an optional originator descriptor (user action, remote control, etc.).
    /// </summary>
    public string? Originator { get; set; }

    /// <summary>
    /// Gets or sets an optional JSON blob describing playback context (shuffle, repeat, resume).
    /// </summary>
    public string? ContextJson { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client believes is current.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }
}
