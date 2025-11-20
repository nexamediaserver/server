// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Core.Services.Authentication;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Data.Interceptors;
using NexaMediaServer.Infrastructure.JobFilters;
using NexaMediaServer.Infrastructure.Repositories;
using NexaMediaServer.Infrastructure.Services;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Authentication;
using NexaMediaServer.Infrastructure.Services.Credits;
using NexaMediaServer.Infrastructure.Services.Hubs;
using NexaMediaServer.Infrastructure.Services.Ignore;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Parts;
using NexaMediaServer.Infrastructure.Services.Pipeline.Stages;
using NexaMediaServer.Infrastructure.Services.Resolvers;
using NexaMediaServer.Infrastructure.Services.Search;
using NexaMediaServer.Infrastructure.Services.Trickplay;
using NexaMediaServer.Infrastructure.Services.Watchers;

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
        builder.Services.Configure<SessionOptions>(
            builder.Configuration.GetSection(SessionOptions.SectionName)
        );
        builder.Services.Configure<TranscodeOptions>(
            builder.Configuration.GetSection(TranscodeOptions.SectionName)
        );
        builder.Services.Configure<JobNotificationOptions>(
            builder.Configuration.GetSection(JobNotificationOptions.SectionName)
        );
        builder.Services.Configure<TagModerationOptions>(
            builder.Configuration.GetSection(TagModerationOptions.SectionName)
        );
        builder.Services.Configure<GenreNormalizationOptions>(
            builder.Configuration.GetSection(GenreNormalizationOptions.SectionName)
        );

        // Register ApplicationPaths as a singleton first, then as a hosted service.
        // This avoids circular dependency when resolving IDbContextFactory, which needs IApplicationPaths,
        // and other hosted services (JobNotificationFlushService) need IDbContextFactory.
        builder.Services.AddSingleton<ApplicationPaths>();
        builder.Services.AddSingleton<IApplicationPaths>(sp =>
            sp.GetRequiredService<ApplicationPaths>()
        );
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ApplicationPaths>());

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

        builder.Services.AddHttpClient();

        builder.Services.AddEasyCaching(options =>
        {
            options.UseInMemory("default");
        });

        builder.Services.AddSingleton<IFfmpegCommandBuilder, FfmpegCommandBuilder>();
        builder.Services.AddScoped<IDashTranscodeService, DashTranscodeService>();

        builder.Services.AddScoped<IMetadataItemRepository, MetadataItemRepository>();
        builder.Services.AddScoped<ILibrarySectionRepository, LibrarySectionRepository>();
        builder.Services.AddScoped<ISectionLocationRepository, SectionLocationRepository>();
        builder.Services.AddScoped<ILibraryScanRepository, LibraryScanRepository>();
        builder.Services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        builder.Services.AddScoped<IMediaPartRepository, MediaPartRepository>();
        builder.Services.AddScoped<IDirectoryRepository, DirectoryRepository>();
        builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
        builder.Services.AddScoped<ISessionRepository, SessionRepository>();
        builder.Services.AddScoped<ISessionService, SessionService>();
        builder.Services.AddScoped<IPlaybackService, PlaybackService>();

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
        builder.Services.AddTransient<IEnumerable<IItemResolver<MetadataBaseItem>>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().ItemResolvers
        );
        builder.Services.AddTransient<IEnumerable<IMetadataAgent>>(sp =>
            sp.GetRequiredService<IPartsRegistry>().MetadataAgents
        );
        builder.Services.AddTransient<DirectoryTraversalStage>();
        builder.Services.AddTransient<ChangeDetectionStage>();
        builder.Services.AddTransient<ResolveItemsStage>();
        builder.Services.AddTransient<LocalMetadataStage>();
        builder.Services.AddScoped<IFileScanner, FileScanner>();
        builder.Services.AddScoped<ILibraryScannerService, LibraryScannerService>();
        builder.Services.AddScoped<IMetadataService, MetadataService>();
        builder.Services.AddScoped<IMetadataRefreshService, MetadataRefreshService>();

        // Metadata processing services (refactored from MetadataService)
        builder.Services.AddScoped<ICreditService, CreditService>();
        builder.Services.AddScoped<IImageOrchestrationService, ImageOrchestrationService>();
        builder.Services.AddScoped<ISidecarMetadataService, SidecarMetadataService>();
        builder.Services.AddScoped<IFileAnalysisOrchestrator, FileAnalysisOrchestrator>();
        builder.Services.AddScoped<IMetadataRefreshOrchestrator, MetadataRefreshOrchestrator>();

        // Genre and tag services
        builder.Services.AddSingleton<IGenreNormalizationService, GenreNormalizationService>();
        builder.Services.AddSingleton<ITagModerationService, TagModerationService>();

        // Content rating service
        builder.Services.AddSingleton<IContentRatingService, ContentRatingService>();

        // Job monitoring and notification system
        builder.Services.AddSingleton<IJobProgressReporter, JobProgressReporter>();
        builder.Services.AddSingleton<JobStateNotificationFilter>();
        builder.Services.AddHostedService<JobNotificationFlushService>();

        // Scan recovery service for resuming interrupted scans on startup
        builder.Services.AddHostedService<ScanRecoveryService>();

        // Filesystem watcher services for real-time library monitoring
        builder.Services.AddSingleton<WatcherEventBuffer>();
        builder.Services.AddSingleton<ILibraryWatcherService, LibraryWatcherService>();
        builder.Services.AddHostedService<LibraryWatcherHostedService>();
        builder.Services.AddScoped<IMicroScanJob, MicroScanJob>();

        // Hub services for themed content discovery
        builder.Services.AddSingleton<IHubDefinitionProvider, HubDefinitionProvider>();
        builder.Services.AddScoped<IHubService, HubService>();

        // Search services for full-text search using Lucene.NET
        builder.Services.AddSingleton<LuceneSearchService>();
        builder.Services.AddSingleton<ISearchService>(sp =>
            sp.GetRequiredService<LuceneSearchService>()
        );
        builder.Services.AddHostedService<SearchIndexHostedService>();
        builder.Services.AddScoped<RebuildSearchIndexJob>();
    }
}
