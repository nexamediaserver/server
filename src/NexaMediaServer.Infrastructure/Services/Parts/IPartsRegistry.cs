// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
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
    IReadOnlyList<IItemResolver> ItemResolvers { get; }

    /// <summary>
    /// Gets the discovered metadata agents (local and remote).
    /// </summary>
    IReadOnlyList<IMetadataAgent> MetadataAgents { get; }

    /// <summary>
    /// Gets the discovered file analyzers ordered by <see cref="Analysis.IFileAnalyzer.Order"/> ascending.
    /// </summary>
    IReadOnlyList<IFileAnalyzer> FileAnalyzers { get; }

    /// <summary>
    /// Gets the discovered image providers ordered by <see cref="Images.IImageProvider.Order"/> ascending.
    /// </summary>
    IReadOnlyList<IImageProvider> ImageProviders { get; }
}
