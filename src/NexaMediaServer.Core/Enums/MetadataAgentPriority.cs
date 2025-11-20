// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Enums;

/// <summary>
/// Defines default execution priority levels for metadata agents.
/// Lower values execute first. Used by IHasOrder implementations
/// to establish a predictable default order when users haven't customized it.
/// </summary>
/// <remarks>
/// <para>
/// Priority conventions:
/// <list type="bullet">
///   <item><description><see cref="Sidecar"/> (10): Local sidecar files (.nfo, metadata.json) - highest priority as explicit user overrides.</description></item>
///   <item><description><see cref="Embedded"/> (20): Embedded metadata (ID3, Matroska tags, MP4 atoms) - bundled with media files.</description></item>
///   <item><description><see cref="Local"/> (30): Local-only metadata agents that don't make network calls.</description></item>
///   <item><description><see cref="Remote"/> (50): Remote metadata agents that fetch from external APIs (TMDB, TVDB, etc.).</description></item>
///   <item><description><see cref="Fallback"/> (90): Last-resort providers for when other sources fail.</description></item>
/// </list>
/// </para>
/// <para>
/// Plugin authors should choose the priority that best matches their agent's behavior.
/// Values between the defined constants are valid for fine-grained ordering within a category.
/// </para>
/// </remarks>
public enum MetadataAgentPriority
{
    /// <summary>
    /// Sidecar file parsers (.nfo, metadata.json, etc.).
    /// These represent explicit user overrides and run first.
    /// </summary>
    Sidecar = 10,

    /// <summary>
    /// Embedded metadata extractors (ID3, Matroska, MP4 atoms).
    /// Metadata bundled directly in media containers.
    /// </summary>
    Embedded = 20,

    /// <summary>
    /// Local metadata agents that don't require network access.
    /// File-based analysis, folder structure inference, etc.
    /// </summary>
    Local = 30,

    /// <summary>
    /// Remote metadata agents that fetch from external APIs.
    /// TMDB, TVDB, MusicBrainz, etc.
    /// </summary>
    Remote = 50,

    /// <summary>
    /// Fallback providers used when other sources yield no results.
    /// Filename parsing, generic providers, etc.
    /// </summary>
    Fallback = 90,
}
