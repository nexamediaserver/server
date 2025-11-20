// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Represents the output of a DASH transcode job.
/// </summary>
/// <param name="ManifestPath">Absolute path to the generated manifest.</param>
/// <param name="OutputDirectory">Directory containing manifest and segments.</param>
public sealed record DashTranscodeResult(string ManifestPath, string OutputDirectory);
