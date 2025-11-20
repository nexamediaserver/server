// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace NexaMediaServer.Core.Services.Pipeline;

/// <summary>
/// Hardcoded concurrency and sizing defaults for scan stages. These will later move to database-backed server settings.
/// </summary>
public static class ScanPipelineConcurrency
{
    /// <summary>
    /// Maximum number of directory traversal producers per library location.
    /// </summary>
    public const int DiscoveryProducers = 2;

    /// <summary>
    /// Maximum parallel metadata agent calls.
    /// </summary>
    public const int AgentConcurrency = 3;

    /// <summary>
    /// Computes resolver worker count based on CPU.
    /// </summary>
    /// <param name="processorCount">Logical processor count.</param>
    /// <returns>Recommended resolver worker count.</returns>
    public static int ResolverWorkers(int processorCount) =>
        Math.Max(2, (int)Math.Floor(processorCount * 0.75));

    /// <summary>
    /// Computes file analyzer worker count.
    /// </summary>
    /// <param name="processorCount">Logical processor count.</param>
    /// <returns>Recommended file analyzer worker count.</returns>
    public static int FileAnalyzerWorkers(int processorCount) =>
        Math.Clamp(processorCount / 2, 2, 4);

    /// <summary>
    /// Computes image provider worker count.
    /// </summary>
    /// <param name="processorCount">Logical processor count.</param>
    /// <returns>Recommended image provider worker count.</returns>
    public static int ImageWorkers(int processorCount) => Math.Max(1, processorCount / 2);
}
