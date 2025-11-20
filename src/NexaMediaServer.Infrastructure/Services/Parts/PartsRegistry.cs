// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Parts;

/// <summary>
/// Thread-safe registry storing discovered parts (rules, resolvers). Designed for one-time population then read-only.
/// </summary>
public sealed class PartsRegistry : IPartsRegistry
{
    private readonly ConcurrentBag<IScannerIgnoreRule> scannerIgnoreRules = new();
    private readonly ConcurrentBag<IItemResolver> itemResolvers = new();
    private readonly ConcurrentBag<IMetadataAgent> metadataAgents = new();
    private readonly ConcurrentBag<IFileAnalyzer> fileAnalyzers = new();
    private readonly ConcurrentBag<IImageProvider> imageProviders = new();
    private volatile bool frozen;
    private IReadOnlyList<IScannerIgnoreRule> cachedIgnoreRules = Array.Empty<IScannerIgnoreRule>();
    private IReadOnlyList<IItemResolver> cachedResolvers = Array.Empty<IItemResolver>();
    private IReadOnlyList<IMetadataAgent> cachedMetadataAgents = Array.Empty<IMetadataAgent>();
    private IReadOnlyList<IFileAnalyzer> cachedFileAnalyzers = Array.Empty<IFileAnalyzer>();
    private IReadOnlyList<IImageProvider> cachedImageProviders = Array.Empty<IImageProvider>();

    /// <inheritdoc />
    public IReadOnlyList<IScannerIgnoreRule> ScannerIgnoreRules => this.cachedIgnoreRules;

    /// <inheritdoc />
    public IReadOnlyList<IItemResolver> ItemResolvers => this.cachedResolvers;

    /// <inheritdoc />
    public IReadOnlyList<IMetadataAgent> MetadataAgents => this.cachedMetadataAgents;

    /// <inheritdoc />
    public IReadOnlyList<IFileAnalyzer> FileAnalyzers => this.cachedFileAnalyzers;

    /// <inheritdoc />
    public IReadOnlyList<IImageProvider> ImageProviders => this.cachedImageProviders;

    /// <summary>
    /// Adds a scanner ignore rule if the registry is not frozen and an equivalent type has not already been added.
    /// </summary>
    /// <summary>
    /// Attempts to add a scanner ignore rule instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="rule">The rule instance to add.</param>
    public void TryAddScannerIgnoreRule(IScannerIgnoreRule rule)
    {
        if (this.frozen || rule is null)
        {
            return;
        }

        // Prevent duplicate types (plugins may load same assembly twice accidentally)
        if (this.scannerIgnoreRules.Any(r => r.GetType() == rule.GetType()))
        {
            return;
        }

        this.scannerIgnoreRules.Add(rule);
    }

    /// <summary>
    /// Adds an item resolver if not frozen and a resolver of the same concrete type is not present.
    /// </summary>
    /// <summary>
    /// Attempts to add an item resolver instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="resolver">The resolver instance to add.</param>
    public void TryAddItemResolver(IItemResolver resolver)
    {
        if (this.frozen || resolver is null)
        {
            return;
        }

        if (this.itemResolvers.Any(r => r.GetType() == resolver.GetType()))
        {
            return;
        }

        this.itemResolvers.Add(resolver);
    }

    /// <summary>
    /// Freezes the registry, preventing new additions (idempotent).
    /// </summary>
    public void Freeze()
    {
        // Materialize immutable snapshots and mark frozen.
        if (this.frozen)
        {
            return;
        }

        this.cachedIgnoreRules = this.scannerIgnoreRules.ToArray();
        this.cachedResolvers = this.itemResolvers.OrderBy(r => r.Priority).ToArray();
        this.cachedMetadataAgents = this.metadataAgents.ToArray();
        this.cachedFileAnalyzers = this.fileAnalyzers.OrderBy(a => a.Order).ToArray();
        this.cachedImageProviders = this.imageProviders.OrderBy(p => p.Order).ToArray();
        this.frozen = true;
    }

    /// <summary>
    /// Attempts to add a metadata agent instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="agent">The agent instance.</param>
    public void TryAddMetadataAgent(IMetadataAgent agent)
    {
        if (this.frozen || agent is null)
        {
            return;
        }

        if (this.metadataAgents.Any(a => a.GetType() == agent.GetType()))
        {
            return;
        }

        this.metadataAgents.Add(agent);
    }

    /// <summary>
    /// Attempts to add a file analyzer instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="analyzer">The analyzer instance.</param>
    public void TryAddFileAnalyzer(IFileAnalyzer analyzer)
    {
        if (this.frozen || analyzer is null)
        {
            return;
        }

        if (this.fileAnalyzers.Any(a => a.GetType() == analyzer.GetType()))
        {
            return;
        }

        this.fileAnalyzers.Add(analyzer);
    }

    /// <summary>
    /// Attempts to add an image provider instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="provider">The image provider instance.</param>
    public void TryAddImageProvider(IImageProvider provider)
    {
        if (this.frozen || provider is null)
        {
            return;
        }

        if (this.imageProviders.Any(p => p.GetType() == provider.GetType()))
        {
            return;
        }

        this.imageProviders.Add(provider);
    }
}
