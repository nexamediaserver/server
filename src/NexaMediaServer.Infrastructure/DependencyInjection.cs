// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Data.Interceptors;
using NexaMediaServer.Infrastructure.Repositories;
using NexaMediaServer.Infrastructure.Services;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Resolvers;
using NexaMediaServer.Infrastructure.Services.Trickplay;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register infrastructure services for the application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services required by the application, including database context, identity, interceptors, and authorization policies.
    /// </summary>
    /// <param name="builder">The application host builder to configure services for.</param>
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        // Register configuration options
        builder.Services.Configure<TrickplayOptions>(
            builder.Configuration.GetSection(TrickplayOptions.SectionName)
        );

        builder
            .Services.AddHostedService<ApplicationPaths>()
            .AddSingleton<IApplicationPaths>(sp =>
                sp.GetServices<IHostedService>().OfType<ApplicationPaths>().First()
            );

        builder.Services.AddPooledDbContextFactory<MediaServerContext>(
            (serviceProvider, options) =>
            {
                var paths = serviceProvider.GetRequiredService<IApplicationPaths>();
                // Ensure the database directory exists before configuring SQLite path
                paths.EnsureDirectoryExists(paths.DatabaseDirectory);
                var dbPath = Path.Combine(paths.DatabaseDirectory, "nexa.db");

                options.UseSqlite(
                    $"Data Source={dbPath}",
                    builder =>
                    {
                        builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        builder.MigrationsAssembly("NexaMediaServer.Infrastructure");
                    }
                );
                options.AddInterceptors(
                    new SoftDeleteInterceptor(),
                    new AuditTimestampsInterceptor(),
                    new SqliteNaturalSortInterceptor()
                );
                options.EnableDetailedErrors();

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                }
            }
        );
        builder.Services.AddDbContextPool<MediaServerContext>(
            (serviceProvider, options) =>
            {
                var paths = serviceProvider.GetRequiredService<IApplicationPaths>();
                // Ensure the database directory exists before configuring SQLite path
                paths.EnsureDirectoryExists(paths.DatabaseDirectory);
                var dbPath = Path.Combine(paths.DatabaseDirectory, "nexa.db");

                options.UseSqlite(
                    $"Data Source={dbPath}",
                    builder =>
                    {
                        builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        builder.MigrationsAssembly("NexaMediaServer.Infrastructure");
                    }
                );
                options.AddInterceptors(
                    new SoftDeleteInterceptor(),
                    new AuditTimestampsInterceptor(),
                    new SqliteNaturalSortInterceptor()
                );
                options.EnableDetailedErrors();

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                }
            }
        );

        builder.Services.AddMemoryCache();

        builder.Services.AddEasyCaching(options =>
        {
            options.UseInMemory("default");
        });

        builder.Services.AddScoped<IMetadataItemRepository, MetadataItemRepository>();
        builder.Services.AddScoped<ILibrarySectionRepository, LibrarySectionRepository>();
        builder.Services.AddScoped<ISectionLocationRepository, SectionLocationRepository>();
        builder.Services.AddScoped<ILibraryScanRepository, LibraryScanRepository>();
        builder.Services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        builder.Services.AddScoped<IMediaPartRepository, MediaPartRepository>();
        builder.Services.AddScoped<IDirectoryRepository, DirectoryRepository>();

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // Media analysis helpers, should be instantiated before the parts discovery.
        // Singleton is appropriate here because the service is stateless and used by long-lived parts discovered at startup.
        builder.Services.AddSingleton<IGopIndexService, GopIndexService>();
        builder.Services.AddSingleton<IBifService, BifService>();
        builder.Services.AddSingleton<IImageService, ImageService>();
        builder.Services.AddSingleton<IFileSystemBrowserService, FileSystemBrowserService>();

        // Extensibility parts discovery (ignore rules, resolvers) via hosted service & registry.
        builder.Services.AddSingleton<PartsRegistry>();
        builder.Services.AddSingleton<IPartsRegistry>(sp => sp.GetRequiredService<PartsRegistry>());
        builder.Services.AddHostedService<PartsDiscoveryHostedService>();
        // Consumers resolve IPartsRegistry and access collections directly.
        builder.Services.AddTransient<IEnumerable<IScannerIgnoreRule>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().ScannerIgnoreRules
        );
        builder.Services.AddTransient<IEnumerable<IItemResolver>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().ItemResolvers
        );
        builder.Services.AddTransient<IEnumerable<IMetadataAgent>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().MetadataAgents
        );
        builder.Services.AddTransient<IEnumerable<IFileAnalyzer>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().FileAnalyzers
        );
        builder.Services.AddTransient<IEnumerable<IImageProvider>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().ImageProviders
        );
        builder.Services.AddScoped<IFileScanner, FileScanner>();
        builder.Services.AddScoped<ILibraryScannerService, LibraryScannerService>();
        builder.Services.AddScoped<IMetadataService, MetadataService>();

        // Job monitoring and notification system
        builder.Services.AddHostedService<JobMonitorHostedService>();
    }
}
