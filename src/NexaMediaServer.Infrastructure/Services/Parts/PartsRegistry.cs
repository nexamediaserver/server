// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Linq;
using NexaMediaServer.Core.DTOs.Metadata;
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
    private readonly ConcurrentBag<IItemResolver<MetadataBaseItem>> itemResolvers = new();
    private readonly ConcurrentBag<IMetadataAgent> metadataAgents = new();
    private readonly ConcurrentDictionary<
        (Type AnalyzerType, Type MetadataType),
        byte
    > fileAnalyzerKeys = new();
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> fileAnalyzers = new();
    private readonly ConcurrentDictionary<
        (Type ProviderType, Type MetadataType),
        byte
    > imageProviderKeys = new();
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> imageProviders = new();
    private volatile bool frozen;
    private IReadOnlyList<IScannerIgnoreRule> cachedIgnoreRules = Array.Empty<IScannerIgnoreRule>();
    private IReadOnlyList<IItemResolver<MetadataBaseItem>> cachedResolvers = Array.Empty<
        IItemResolver<MetadataBaseItem>
    >();
    private IReadOnlyList<IMetadataAgent> cachedMetadataAgents = Array.Empty<IMetadataAgent>();
    private IReadOnlyDictionary<Type, IReadOnlyList<object>> cachedFileAnalyzers =
        new Dictionary<Type, IReadOnlyList<object>>();
    private IReadOnlyDictionary<Type, IReadOnlyList<object>> cachedImageProviders =
        new Dictionary<Type, IReadOnlyList<object>>();

    /// <inheritdoc />
    public IReadOnlyList<IScannerIgnoreRule> ScannerIgnoreRules => this.cachedIgnoreRules;

    /// <inheritdoc />
    public IReadOnlyList<IItemResolver<MetadataBaseItem>> ItemResolvers => this.cachedResolvers;

    /// <inheritdoc />
    public IReadOnlyList<IMetadataAgent> MetadataAgents => this.cachedMetadataAgents;

    /// <summary>
    /// Gets the total count of registered file analyzers across all metadata types.
    /// </summary>
    internal int FileAnalyzerCount => this.cachedFileAnalyzers.Values.Sum(list => list.Count);

    /// <summary>
    /// Gets the total count of registered image providers across all metadata types.
    /// </summary>
    internal int ImageProviderCount => this.cachedImageProviders.Values.Sum(list => list.Count);

    /// <inheritdoc />
    public IReadOnlyList<IFileAnalyzer<TMetadata>> GetFileAnalyzers<TMetadata>()
        where TMetadata : MetadataBaseItem
    {
        var metadataType = typeof(TMetadata);
        var analyzers = new List<IFileAnalyzer<TMetadata>>();

        foreach (var kvp in this.cachedFileAnalyzers)
        {
            if (!kvp.Key.IsAssignableFrom(metadataType))
            {
                continue;
            }

            foreach (var analyzer in kvp.Value)
            {
                if (analyzer is IFileAnalyzer<TMetadata> typedAnalyzer)
                {
                    analyzers.Add(typedAnalyzer);
                }
            }
        }

        if (analyzers.Count == 0)
        {
            return Array.Empty<IFileAnalyzer<TMetadata>>();
        }

        return analyzers.OrderBy(static analyzer => GetOrder(analyzer)).ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<IImageProvider<TMetadata>> GetImageProviders<TMetadata>()
        where TMetadata : MetadataBaseItem
    {
        var metadataType = typeof(TMetadata);
        var providers = new List<IImageProvider<TMetadata>>();
        var seen = new HashSet<object>();

        foreach (var kvp in this.cachedImageProviders)
        {
            if (!kvp.Key.IsAssignableFrom(metadataType))
            {
                continue;
            }

            foreach (var provider in kvp.Value)
            {
                if (!seen.Add(provider))
                {
                    continue;
                }

                if (provider is IImageProvider<TMetadata> typedProvider)
                {
                    providers.Add(typedProvider);
                }
            }
        }

        if (providers.Count == 0)
        {
            return Array.Empty<IImageProvider<TMetadata>>();
        }

        return providers.OrderBy(static provider => GetOrder(provider)).ToArray();
    }

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
    public void TryAddItemResolver(IItemResolver<MetadataBaseItem> resolver)
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
        this.cachedFileAnalyzers = this.fileAnalyzers.ToDictionary(
            static kvp => kvp.Key,
            kvp =>
                (IReadOnlyList<object>)
                    kvp.Value.OrderBy(static instance => GetOrder(instance)).ToArray()
        );
        this.cachedImageProviders = this.imageProviders.ToDictionary(
            static kvp => kvp.Key,
            kvp =>
                (IReadOnlyList<object>)
                    kvp.Value.OrderBy(static instance => GetOrder(instance)).ToArray()
        );
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
    /// <param name="metadataType">The metadata DTO type the analyzer targets.</param>
    /// <param name="analyzer">The analyzer instance.</param>
    internal void TryAddFileAnalyzer(Type metadataType, object analyzer)
    {
        if (this.frozen || analyzer is null)
        {
            return;
        }

        var key = (analyzer.GetType(), metadataType);
        if (!this.fileAnalyzerKeys.TryAdd(key, 0))
        {
            return;
        }

        var bag = this.fileAnalyzers.GetOrAdd(metadataType, _ => new ConcurrentBag<object>());
        bag.Add(analyzer);
    }

    /// <summary>
    /// Attempts to add an image provider instance if not already present and registry not frozen.
    /// </summary>
    /// <param name="metadataType">The metadata DTO type the provider targets.</param>
    /// <param name="provider">The image provider instance.</param>
    internal void TryAddImageProvider(Type metadataType, object provider)
    {
        if (this.frozen || provider is null)
        {
            return;
        }

        var key = (provider.GetType(), metadataType);
        if (!this.imageProviderKeys.TryAdd(key, 0))
        {
            return;
        }

        var bag = this.imageProviders.GetOrAdd(metadataType, _ => new ConcurrentBag<object>());
        bag.Add(provider);
    }

    private static int GetOrder(object instance)
    {
        const string orderPropertyName = nameof(IImageProvider<MetadataBaseItem>.Order);
        var property = instance.GetType().GetProperty(orderPropertyName);
        if (property?.GetValue(instance) is int order)
        {
            return order;
        }

        return 0;
    }
}
