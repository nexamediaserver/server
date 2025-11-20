// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;

namespace NexaMediaServer.Core.Services.Pipeline;

/// <summary>
/// Entry points for constructing scan pipelines.
/// </summary>
public static class ScanPipeline
{
    /// <summary>
    /// Starts a pipeline from an input source factory.
    /// </summary>
    /// <typeparam name="TCurrent">The stream item type.</typeparam>
    /// <param name="sourceFactory">Factory that yields the source stream.</param>
    /// <returns>Pipeline builder seeded with the source stream.</returns>
    public static ScanPipelineBuilder<TCurrent> From<TCurrent>(
        Func<IPipelineContext, CancellationToken, IAsyncEnumerable<TCurrent>> sourceFactory
    ) => new(sourceFactory);

    /// <summary>
    /// Starts a pipeline from an existing asynchronous sequence.
    /// </summary>
    /// <typeparam name="TCurrent">The stream item type.</typeparam>
    /// <param name="source">Existing asynchronous sequence.</param>
    /// <returns>Pipeline builder seeded with the source stream.</returns>
    public static ScanPipelineBuilder<TCurrent> From<TCurrent>(IAsyncEnumerable<TCurrent> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new((_, _) => source);
    }
}
