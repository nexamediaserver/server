// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Playback;

/// <summary>
/// Contains all available and selected tracks for a playback session.
/// </summary>
public sealed class TrackSelection
{
    /// <summary>
    /// Gets or sets all available audio tracks.
    /// </summary>
    public List<AudioTrackInfo> AudioTracks { get; set; } = [];

    /// <summary>
    /// Gets or sets all available subtitle tracks.
    /// </summary>
    public List<SubtitleTrackInfo> SubtitleTracks { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected audio track index.
    /// </summary>
    public int? SelectedAudioIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected subtitle track index.
    /// </summary>
    public int? SelectedSubtitleIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle selection was made by the user.
    /// </summary>
    public bool SubtitleUserSelected { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio selection was made by the user.
    /// </summary>
    public bool AudioUserSelected { get; set; }
}
