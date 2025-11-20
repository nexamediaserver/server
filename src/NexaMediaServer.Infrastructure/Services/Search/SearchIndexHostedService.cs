// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Search;

/// <summary>
/// Hosted service responsible for managing the search index lifecycle.
/// Verifies schema version on startup and triggers rebuilds when necessary.
/// </summary>
public sealed partial class SearchIndexHostedService : IHostedService
{
    private readonly ISearchService searchService;
    private readonly ILogger<SearchIndexHostedService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexHostedService"/> class.
    /// </summary>
    /// <param name="searchService">The search service.</param>
    /// <param name="logger">The logger.</param>
    public SearchIndexHostedService(
        ISearchService searchService,
        ILogger<SearchIndexHostedService> logger
    )
    {
        this.searchService = searchService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.LogStarting();

        try
        {
            var currentVersion = this.searchService.GetIndexSchemaVersion();
            var expectedVersion = this.searchService.ExpectedSchemaVersion;

            if (currentVersion == null)
            {
                this.LogNoIndex();
                await this.ScheduleFullRebuildAsync();
            }
            else if (currentVersion != expectedVersion)
            {
                this.LogSchemaVersionMismatch(currentVersion.Value, expectedVersion);
                await this.ScheduleFullRebuildAsync();
            }
            else
            {
                this.LogIndexUpToDate(expectedVersion);
            }
        }
        catch (Exception ex)
        {
            this.LogStartupError(ex);
            // Don't throw - allow the application to continue without search
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.LogStopping();
        return Task.CompletedTask;
    }

    private Task ScheduleFullRebuildAsync()
    {
        this.LogSchedulingRebuild();

        // Schedule a background job for full index rebuild
        BackgroundJob.Enqueue<RebuildSearchIndexJob>(job =>
            job.ExecuteAsync(CancellationToken.None)
        );

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Search index hosted service starting")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Search index hosted service stopping")]
    private partial void LogStopping();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No search index found, scheduling full rebuild"
    )]
    private partial void LogNoIndex();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Search index schema version mismatch (current: {CurrentVersion}, expected: {ExpectedVersion}), scheduling full rebuild"
    )]
    private partial void LogSchemaVersionMismatch(int currentVersion, int expectedVersion);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Search index is up to date (version {Version})"
    )]
    private partial void LogIndexUpToDate(int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduling full search index rebuild")]
    private partial void LogSchedulingRebuild();

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error during search index hosted service startup"
    )]
    private partial void LogStartupError(Exception ex);
}
