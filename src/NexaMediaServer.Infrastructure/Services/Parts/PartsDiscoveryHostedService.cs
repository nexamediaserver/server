// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Parts;

/// <summary>
/// Discovers implementations of extensibility interfaces (<see cref="IScannerIgnoreRule"/>, <see cref="IItemResolver"/>) across core assemblies and plugin assemblies, registering them into <see cref="PartsRegistry"/>.
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

            this.registry.Freeze();
            this.LogDiscoverySummary(
                this.registry.ScannerIgnoreRules.Count,
                this.registry.ItemResolvers.Count,
                this.registry.MetadataAgents.Count,
                this.registry.FileAnalyzers.Count,
                this.registry.ImageProviders.Count
            );
        }
        catch (Exception ex)
        {
            this.LogDiscoveryFailed(ex);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
                    this.LogPartDiscovered(type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPartDiscoveryError(type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverItemResolvers(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IItemResolver).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IItemResolver?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddItemResolver(instance);
                    this.LogPartDiscovered(type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPartDiscoveryError(type.FullName ?? type.Name, ex);
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
                    this.LogPartDiscovered(type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPartDiscoveryError(type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverFileAnalyzers(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IFileAnalyzer).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IFileAnalyzer?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddFileAnalyzer(instance);
                    this.LogPartDiscovered(type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPartDiscoveryError(type.FullName ?? type.Name, ex);
            }
        }
    }

    private void DiscoverImageProviders(IEnumerable<Assembly> assemblies)
    {
        foreach (var type in assemblies.SelectMany(SafeGetTypes))
        {
            if (
                !typeof(IImageProvider).IsAssignableFrom(type)
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericTypeDefinition
            )
            {
                continue;
            }

            try
            {
                var instance = (IImageProvider?)
                    ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                if (instance != null)
                {
                    this.registry.TryAddImageProvider(instance);
                    this.LogPartDiscovered(type.FullName ?? type.Name);
                }
            }
            catch (Exception ex)
            {
                this.LogPartDiscoveryError(type.FullName ?? type.Name, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Discovered part: {TypeName}")]
    private partial void LogPartDiscovered(string TypeName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error instantiating part {TypeName}")]
    private partial void LogPartDiscoveryError(string TypeName, Exception Message);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded plugin assembly: {AssemblyName}"
    )]
    private partial void LogPluginAssemblyLoaded(string AssemblyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load plugin assembly {Path}")]
    private partial void LogPluginLoadFailed(string Path, Exception Message);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Parts discovery completed. IgnoreRules={IgnoreCount}, ItemResolvers={ResolverCount}, MetadataAgents={AgentCount}, FileAnalyzers={AnalyzerCount}, ImageProviders={ImageProviderCount}"
    )]
    private partial void LogDiscoverySummary(
        int IgnoreCount,
        int ResolverCount,
        int AgentCount,
        int AnalyzerCount,
        int ImageProviderCount
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Parts discovery failed")]
    private partial void LogDiscoveryFailed(Exception Message);
}
