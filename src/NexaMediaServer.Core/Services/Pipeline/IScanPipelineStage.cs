// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;

namespace NexaMediaServer.Core.Services.Pipeline;

/// <summary>
/// A stage within the scan pipeline. Implementations should be stateless and safe for reuse.
/// </summary>
/// <typeparam name="TIn">Input item type.</typeparam>
/// <typeparam name="TOut">Output item type.</typeparam>
public interface IScanPipelineStage<TIn, TOut>
{
    /// <summary>
    /// Gets a short human-friendly name used for logging/progress.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the stage over the input stream.
    /// </summary>
    /// <param name="input">Asynchronous input stream.</param>
    /// <param name="context">Scan context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous output stream.</returns>
    IAsyncEnumerable<TOut> ExecuteAsync(
        IAsyncEnumerable<TIn> input,
        IPipelineContext context,
        CancellationToken cancellationToken
    );
}
