// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload describing the server decision for playback continuation.
/// </summary>
public sealed class PlaybackDecisionPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackDecisionPayload"/> class.
    /// </summary>
    /// <param name="response">Decision response from the service.</param>
    /// <param name="nextItemId">Next metadata item identifier, if provided.</param>
    /// <param name="capabilityProfileVersion">Latest capability profile version.</param>
    /// <param name="capabilityVersionMismatch">Indicates whether the client's capability version is stale.</param>
    public PlaybackDecisionPayload(
        PlaybackDecisionResponse response,
        Guid? nextItemId,
        int capabilityProfileVersion,
        bool capabilityVersionMismatch
    )
    {
        this.Action = response.Action;
        this.StreamPlanJson = response.StreamPlanJson;
        this.NextItemId = nextItemId;
        this.CapabilityProfileVersion = capabilityProfileVersion;
        this.PlaybackUrl = response.PlaybackUrl;
        this.TrickplayUrl = response.TrickplayUrl;
        this.CapabilityVersionMismatch = capabilityVersionMismatch;
    }

    /// <summary>
    /// Gets the action the client should take.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the serialized stream plan for the next item.
    /// </summary>
    public string StreamPlanJson { get; }

    /// <summary>
    /// Gets the next metadata item identifier.
    /// </summary>
    [ID("Item")]
    public Guid? NextItemId { get; }

    /// <summary>
    /// Gets the URL the client should load for the decided item.
    /// </summary>
    public string PlaybackUrl { get; }

    /// <summary>
    /// Gets the trickplay thumbnail track URL when available.
    /// </summary>
    public string? TrickplayUrl { get; }

    /// <summary>
    /// Gets the latest capability profile version known to the server.
    /// </summary>
    public int CapabilityProfileVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the client should refresh capabilities.
    /// </summary>
    public bool CapabilityVersionMismatch { get; }
}
