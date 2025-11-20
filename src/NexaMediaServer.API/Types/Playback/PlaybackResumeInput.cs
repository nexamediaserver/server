// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Input payload used to resume an existing playback session.
/// </summary>
public sealed class PlaybackResumeInput
{
    /// <summary>
    /// Gets or sets the playback session identifier.
    /// </summary>
    public Guid PlaybackSessionId { get; set; }

    /// <summary>
    /// Gets or sets the capability profile version the client is using.
    /// </summary>
    public int? CapabilityProfileVersion { get; set; }

    /// <summary>
    /// Gets or sets an optional capability declaration to upsert.
    /// </summary>
    public PlaybackCapabilityInput? Capability { get; set; }
}
