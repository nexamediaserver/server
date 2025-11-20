// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Watchers;

/// <summary>
/// Hosted service that manages the lifecycle of library filesystem watchers.
/// Starts watchers on application startup and handles library configuration changes.
/// </summary>
public sealed partial class LibraryWatcherHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILibraryWatcherService watcherService;
    private readonly ILogger<LibraryWatcherHostedService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryWatcherHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory for creating scoped services.</param>
    /// <param name="watcherService">The library watcher service.</param>
    /// <param name="logger">The logger instance.</param>
    public LibraryWatcherHostedService(
        IServiceScopeFactory scopeFactory,
        ILibraryWatcherService watcherService,
        ILogger<LibraryWatcherHostedService> logger
    )
    {
        this.scopeFactory = scopeFactory;
        this.watcherService = watcherService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogStartingWatchers(this.logger);

        try
        {
            await using var scope = this.scopeFactory.CreateAsyncScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<MediaServerContext>
            >();

            await using var context = await dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Load all library sections with their locations and settings
            var libraries = await context
                .LibrarySections.Include(l => l.Locations)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var watchersStarted = 0;
            var watchersSkipped = 0;

            foreach (var library in libraries)
            {
                if (!library.Settings.WatcherEnabled)
                {
                    watchersSkipped++;
                    continue;
                }

                try
                {
                    await this
                        .watcherService.StartWatchingAsync(library, cancellationToken)
                        .ConfigureAwait(false);
                    watchersStarted++;
                }
                catch (Exception ex)
                {
                    LogWatcherStartError(this.logger, library.Id, library.Name, ex);
                    // Continue with other libraries
                }
            }

            LogWatchersStarted(this.logger, watchersStarted, watchersSkipped);
        }
        catch (Exception ex)
        {
            LogStartupError(this.logger, ex);
            // Don't throw - allow the application to continue without watchers
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogStoppingWatchers(this.logger);

        // The watchers will be disposed when LibraryWatcherService is disposed by DI
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting library filesystem watchers..."
    )]
    private static partial void LogStartingWatchers(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Library filesystem watchers started: {Started} active, {Skipped} disabled"
    )]
    private static partial void LogWatchersStarted(ILogger logger, int started, int skipped);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to start watcher for library {LibraryId} ({LibraryName})"
    )]
    private static partial void LogWatcherStartError(
        ILogger logger,
        int libraryId,
        string libraryName,
        Exception ex
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during watcher startup")]
    private static partial void LogStartupError(ILogger logger, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stopping library filesystem watchers..."
    )]
    private static partial void LogStoppingWatchers(ILogger logger);
}
