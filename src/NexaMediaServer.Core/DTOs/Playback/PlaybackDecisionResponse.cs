// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Response describing the server decision for continuation.
/// </summary>
public sealed class PlaybackDecisionResponse
{
    /// <summary>
    /// Gets or sets the action the client should take (continue, stop, prompt, refresh).
    /// </summary>
    public string Action { get; set; } = "continue";

    /// <summary>
    /// Gets or sets the serialized stream plan for the next item.
    /// </summary>
    public string StreamPlanJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the next metadata item identifier, if any.
    /// </summary>
    public int? NextMetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the next metadata item's public UUID, if any.
    /// </summary>
    public Guid? NextMetadataItemUuid { get; set; }

    /// <summary>
    /// Gets or sets the URL the client should load for the decided item.
    /// </summary>
    public string PlaybackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trickplay thumbnail track URL, if available.
    /// </summary>
    public string? TrickplayUrl { get; set; }

    /// <summary>
    /// Gets or sets the current capability profile version for the session.
    /// </summary>
    public int CapabilityProfileVersion { get; set; }
}
