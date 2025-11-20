// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Helper utilities for <see cref="LocalMetadataStage"/>.
/// </summary>
public sealed partial class LocalMetadataStage
{
    private static IReadOnlyDictionary<string, object>? MergeHints(
        IReadOnlyDictionary<string, object>? existing,
        IReadOnlyDictionary<string, object>? incoming
    )
    {
        if (incoming is null || incoming.Count == 0)
        {
            return existing;
        }

        if (existing is null || existing.Count == 0)
        {
            return incoming;
        }

        var merged = new Dictionary<string, object>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in incoming)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    private static IEnumerable<FileSystemMetadata> EnumerateSidecarCandidates(
        FileSystemMetadata mediaFile
    )
    {
        var directoryPath = Path.GetDirectoryName(mediaFile.Path);
        if (string.IsNullOrEmpty(directoryPath))
        {
            yield break;
        }

        IEnumerable<string> candidates;
        try
        {
            candidates = Directory.EnumerateFiles(directoryPath);
        }
        catch
        {
            yield break;
        }

        foreach (var candidatePath in candidates)
        {
            if (string.Equals(candidatePath, mediaFile.Path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = FileSystemMetadata.FromPath(candidatePath);
            if (!candidate.Exists || candidate.IsDirectory)
            {
                continue;
            }

            yield return candidate;
        }
    }
}
