// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Client capability declaration supplied with playback operations.
/// </summary>
public sealed class PlaybackCapabilityInput
{
    /// <summary>
    /// Gets or sets the client device identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets a friendly device name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the structured playback capabilities payload.
    /// </summary>
    public PlaybackCapabilitiesInput Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the explicit version the client wants to use.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Converts the GraphQL input into a DTO used by the playback service.
    /// </summary>
    /// <returns>A capability profile DTO.</returns>
    internal CapabilityProfileInput ToDto()
    {
        return new CapabilityProfileInput
        {
            DeviceId = this.DeviceId,
            Name = this.Name,
            Capabilities = this.Capabilities?.ToDto() ?? new PlaybackCapabilities(),
            Version = this.Version,
        };
    }
}
