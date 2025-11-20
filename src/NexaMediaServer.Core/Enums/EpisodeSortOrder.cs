// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Defines how episodes should be sorted within a season/series for display and selection.
/// </summary>
public enum EpisodeSortOrder
{
    /// <summary>
    /// Sort by original air date ascending.
    /// </summary>
    AirDate = 0,

    /// <summary>
    /// Sort by season and episode number (SxxExx), ascending.
    /// </summary>
    SeasonEpisode = 1,

    /// <summary>
    /// Sort by production order when available; falls back to air date.
    /// </summary>
    Production = 2,
}
