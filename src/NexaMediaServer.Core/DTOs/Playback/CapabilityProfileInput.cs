// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Client-supplied capability payload used to construct or refresh capability profiles.
/// </summary>
public sealed class CapabilityProfileInput
{
    /// <summary>
    /// Gets or sets the client device identifier when provided.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets a friendly name for the device.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the declarative capability payload supplied by the client.
    /// </summary>
    public PlaybackCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional explicit version requested by the client.
    /// </summary>
    public int? Version { get; set; }
}
