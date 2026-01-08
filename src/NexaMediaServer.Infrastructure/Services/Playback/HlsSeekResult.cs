// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Result of a seek-based HLS transcode operation.
/// </summary>
/// <param name="MasterPlaylistPath">Path to the master m3u8 playlist.</param>
/// <param name="OutputDirectory">Directory containing the playlists and segments.</param>
/// <param name="StartTimeMs">The actual start time of the transcoded content in milliseconds (keyframe-aligned).</param>
/// <param name="VariantPaths">Dictionary of variant ID to variant playlist path.</param>
public sealed record HlsSeekResult(
    string MasterPlaylistPath,
    string OutputDirectory,
    long StartTimeMs,
    Dictionary<string, string> VariantPaths
);
