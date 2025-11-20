// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Parts;

/// <summary>
/// Provides access to discovered extensibility parts (ignore rules, item resolvers, etc.).
/// Backed by a mutable internal collection populated during startup by <see cref="PartsDiscoveryHostedService"/>.
/// </summary>
public interface IPartsRegistry
{
    /// <summary>
    /// Gets the discovered scanner ignore rules. Immutable snapshot once populated.
    /// </summary>
    IReadOnlyList<IScannerIgnoreRule> ScannerIgnoreRules { get; }

    /// <summary>
    /// Gets the discovered item resolvers ordered by priority ascending (lowest first).
    /// </summary>
    IReadOnlyList<IItemResolver<MetadataBaseItem>> ItemResolvers { get; }

    /// <summary>
    /// Gets the discovered metadata agents (local and remote).
    /// </summary>
    IReadOnlyList<IMetadataAgent> MetadataAgents { get; }

    /// <summary>
    /// Gets the discovered sidecar parsers.
    /// </summary>
    IReadOnlyList<ISidecarParser> SidecarParsers { get; }

    /// <summary>
    /// Gets the discovered embedded metadata extractors.
    /// </summary>
    IReadOnlyList<IEmbeddedMetadataExtractor> EmbeddedMetadataExtractors { get; }

    /// <summary>
    /// Gets the discovered file analyzers for the specified metadata type ordered by their <c>Order</c> value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata DTO type.</typeparam>
    /// <returns>The list of file analyzers.</returns>
    IReadOnlyList<IFileAnalyzer<TMetadata>> GetFileAnalyzers<TMetadata>()
        where TMetadata : MetadataBaseItem;

    /// <summary>
    /// Gets the discovered image providers for the specified metadata type ordered by their <c>Order</c> value.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata DTO type.</typeparam>
    /// <returns>The list of image providers.</returns>
    IReadOnlyList<IImageProvider<TMetadata>> GetImageProviders<TMetadata>()
        where TMetadata : MetadataBaseItem;
}
