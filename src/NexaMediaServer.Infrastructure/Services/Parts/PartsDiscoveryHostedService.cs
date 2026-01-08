// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Parts;

/// <summary>
/// Discovers implementations of extensibility interfaces (<see cref="IScannerIgnoreRule"/>, <see cref="IItemResolver{TMetadata}"/>) across core assemblies and plugin assemblies, registering them into <see cref="PartsRegistry"/>.
/// This mirrors Jellyfin's AddParts concept but adapted for DI + hosted service startup.
/// </summary>
public sealed partial class PartsDiscoveryHostedService : IHostedService
{
    private readonly ILogger<PartsDiscoveryHostedService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly PartsRegistry registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartsDiscoveryHostedService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">Root service provider used for part activation.</param>
    /// <param name="registry">The parts registry to populate.</param>
    public PartsDiscoveryHostedService(
        ILogger<PartsDiscoveryHostedService> logger,
        IServiceProvider serviceProvider,
        PartsRegistry registry
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.registry = registry;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            this.DiscoverScannerIgnoreRules(assemblies);
            this.DiscoverItemResolvers(assemblies);
            this.DiscoverMetadataAgents(assemblies);
            this.DiscoverFileAnalyzers(assemblies);
            this.DiscoverImageProviders(assemblies);
            this.DiscoverSidecarParsers(assemblies);
            this.DiscoverEmbeddedMetadataExtractors(assemblies);

            this.registry.Freeze();
            var summary =
                $"IgnoreRules={this.registry.ScannerIgnoreRules.Count}, "
                + $"ItemResolvers={this.registry.ItemResolvers.Count}, "
                + $"MetadataAgents={this.registry.MetadataAgents.Count}, "
                + $"FileAnalyzers={this.registry.FileAnalyzerCount}, "
                + $"ImageProviders={this.registry.ImageProviderCount}, "
                + $"SidecarParsers={this.registry.SidecarParserCount}, "
                + $"EmbeddedExtractors={this.registry.EmbeddedMetadataExtractorCount}";

            LogDiscoverySummary(this.logger, summary);
        }
        catch (Exception ex)
        {
            LogDiscoveryFailed(this.logger, ex);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool ImplementsGenericInterface(Type type, Type openGeneric)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
    }

    private static List<Type> GetGenericInterfaceImplementations(Type type, Type openGeneric)
    {
        return type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
            .ToList();
    }

    private static bool ShouldSkipGenericPart(
        Type type,
        Type openGeneric,
        out List<Type> interfaces
    )
    {
        interfaces = GetGenericInterfaceImplementations(type, openGeneric);

        if (interfaces.Count == 0)
        {
            return true;
        }

        return type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            return rtle.Types.Where(t => t != null)!;
        }
        catch
        {
            return Enumerable.Empty<Type>();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Discovered part: {TypeName}")]
    private static partial void LogPartDiscovered(ILogger logger, string TypeName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error instantiating part {TypeName}")]
    private static partial void LogPartDiscoveryError(
        ILogger logger,
        string TypeName,
        Exception Message
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded plugin assembly: {AssemblyName}"
    )]
    private static partial void LogPluginAssemblyLoaded(ILogger logger, string AssemblyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load plugin assembly {Path}")]
    private static partial void LogPluginLoadFailed(ILogger logger, string Path, Exception Message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parts discovery completed. {Summary}")]
    private static partial void LogDiscoverySummary(ILogger logger, string Summary);

    [LoggerMessage(Level = LogLevel.Error, Message = "Parts discovery failed")]
    private static partial void LogDiscoveryFailed(ILogger logger, Exception Message);

    private void DiscoverScannerIgnoreRules(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IScannerIgnoreRule).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IScannerIgnoreRule?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddScannerIgnoreRule(instance);
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverItemResolvers(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (!ImplementsGenericInterface(type, typeof(IItemResolver<>)))
            {
                continue;
            }

            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            {
                continue;
            }

            try
            {
                var instance = ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance is IItemResolver<MetadataBaseItem> resolver)
                {
                    this.registry.TryAddItemResolver(resolver);
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverMetadataAgents(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IMetadataAgent).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IMetadataAgent?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddMetadataAgent(instance);
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverFileAnalyzers(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (ShouldSkipGenericPart(type, typeof(IFileAnalyzer<>), out var analyzerInterfaces))
            {
                continue;
            }

            try
            {
                object? instance = null;
                foreach (var iface in analyzerInterfaces)
                {
                    var metadataType = iface.GetGenericArguments()[0];
                    if (!typeof(MetadataBaseItem).IsAssignableFrom(metadataType))
                    {
                        continue;
                    }

                    instance ??= ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                    if (instance != null)
                    {
                        this.registry.TryAddFileAnalyzer(metadataType, instance);
                    }
                }

                if (instance != null)
                {
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverImageProviders(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (ShouldSkipGenericPart(type, typeof(IImageProvider<>), out var providerInterfaces))
            {
                continue;
            }

            try
            {
                object? instance = null;
                foreach (var iface in providerInterfaces)
                {
                    var metadataType = iface.GetGenericArguments()[0];
                    if (!typeof(MetadataBaseItem).IsAssignableFrom(metadataType))
                    {
                        continue;
                    }

                    instance ??= ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                    if (instance != null)
                    {
                        this.registry.TryAddImageProvider(metadataType, instance);
                    }
                }

                if (instance != null)
                {
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverSidecarParsers(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(ISidecarParser).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (ISidecarParser?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddSidecarParser(instance);
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverEmbeddedMetadataExtractors(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IEmbeddedMetadataExtractor).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IEmbeddedMetadataExtractor?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddEmbeddedMetadataExtractor(instance);
                    LogPartDiscovered(this.logger, type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                LogPartDiscoveryError(this.logger, type.FullName ?? type.Name, ex);
            }
        }
    }
}
