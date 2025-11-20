// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;

namespace NexaMediaServer.Core.Services.Pipeline;

/// <summary>
/// Composes scan pipeline stages into a typed, asynchronous sequence.
/// </summary>
/// <typeparam name="TCurrent">The current stream item type.</typeparam>
public sealed class ScanPipelineBuilder<TCurrent>
{
    private readonly Func<IPipelineContext, CancellationToken, IAsyncEnumerable<TCurrent>> factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanPipelineBuilder{TCurrent}"/> class.
    /// </summary>
    /// <param name="factory">Factory that yields the current stage stream.</param>
    public ScanPipelineBuilder(
        Func<IPipelineContext, CancellationToken, IAsyncEnumerable<TCurrent>> factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
    }

    /// <summary>
    /// Appends a stage to the pipeline and returns a builder for the next output type.
    /// </summary>
    /// <typeparam name="TNext">The next stage output type.</typeparam>
    /// <param name="stage">Stage instance to append.</param>
    /// <returns>A builder for the next stage output type.</returns>
    public ScanPipelineBuilder<TNext> Then<TNext>(IScanPipelineStage<TCurrent, TNext> stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return new ScanPipelineBuilder<TNext>(
            (context, cancellationToken) =>
                stage.ExecuteAsync(
                    this.factory(context, cancellationToken),
                    context,
                    cancellationToken
                )
        );
    }

    /// <summary>
    /// Builds the pipeline into a runnable asynchronous sequence.
    /// </summary>
    /// <param name="context">Pipeline context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous stream produced by the composed pipeline.</returns>
    public IAsyncEnumerable<TCurrent> Build(
        IPipelineContext context,
        CancellationToken cancellationToken
    ) => this.factory(context, cancellationToken);
}
