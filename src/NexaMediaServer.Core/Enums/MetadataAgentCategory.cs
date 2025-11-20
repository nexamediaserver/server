// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Categorizes metadata agents by their data source type.
/// Used in the UI to display appropriate icons and group agents.
/// </summary>
public enum MetadataAgentCategory
{
    /// <summary>
    /// Sidecar file parsers that read metadata from adjacent files (.nfo, metadata.json).
    /// </summary>
    Sidecar = 1,

    /// <summary>
    /// Embedded metadata extractors that read tags from media containers (ID3, Matroska, MP4).
    /// </summary>
    Embedded = 2,

    /// <summary>
    /// Local metadata agents that derive information without network access.
    /// </summary>
    Local = 3,

    /// <summary>
    /// Remote metadata agents that fetch from external APIs.
    /// </summary>
    Remote = 4,
}
