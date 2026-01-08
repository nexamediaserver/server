// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Result of ensuring HLS/DASH segments are ready.
/// </summary>
/// <param name="PlaylistPath">Path to the playlist file.</param>
/// <param name="OutputDirectory">Directory containing segments.</param>
public sealed record EnsureResult(string PlaylistPath, string OutputDirectory);
