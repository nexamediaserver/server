// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services.Search;

/// <summary>
/// Hangfire job that rebuilds the entire search index from scratch.
/// </summary>
[Queue("search_index")]
public sealed partial class RebuildSearchIndexJob
{
    private const int BatchSize = 500;

    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly ISearchService searchService;
    private readonly ILogger<RebuildSearchIndexJob> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RebuildSearchIndexJob"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="searchService">The search service.</param>
    /// <param name="logger">The logger.</param>
    public RebuildSearchIndexJob(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        ISearchService searchService,
        ILogger<RebuildSearchIndexJob> logger
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.searchService = searchService;
        this.logger = logger;
    }

    /// <summary>
    /// Executes the full search index rebuild.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [JobDisplayName("Rebuild Search Index")]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogRebuildStarted();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // First, clear and prepare the index
            await this.searchService.RebuildIndexAsync(cancellationToken);

            // Get total count for progress reporting
            await using var countContext = await this.dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );
            var totalCount = await countContext.MetadataItems.CountAsync(cancellationToken);

            if (totalCount == 0)
            {
                this.LogNoItemsToIndex();
                return;
            }

            this.LogIndexingItems(totalCount);

            // Process items in batches
            var processedCount = 0;
            var lastId = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var context = await this.dbContextFactory.CreateDbContextAsync(
                    cancellationToken
                );

                var batch = await context
                    .MetadataItems.AsNoTracking()
                    .Where(m => m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Take(BatchSize)
                    .Include(m => m.LibrarySection)
                    .Include(m => m.OutgoingRelations)
                        .ThenInclude(r => r.RelatedMetadataItem)
                    .Include(m => m.IncomingRelations)
                        .ThenInclude(r => r.MetadataItem)
                    .Include(m => m.Genres)
                    .Include(m => m.Tags)
                    .AsSplitQuery()
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                {
                    break;
                }

                await this.searchService.IndexItemsAsync(batch, cancellationToken);

                processedCount += batch.Count;
                lastId = batch[^1].Id;

                this.LogBatchIndexed(processedCount, totalCount);
            }

            stopwatch.Stop();
            this.LogRebuildCompleted(processedCount, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            this.LogRebuildCancelled();
            throw;
        }
        catch (Exception ex)
        {
            this.LogRebuildFailed(ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting search index rebuild")]
    private partial void LogRebuildStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "No metadata items to index")]
    private partial void LogNoItemsToIndex();

    [LoggerMessage(Level = LogLevel.Information, Message = "Indexing {TotalCount} metadata items")]
    private partial void LogIndexingItems(int totalCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Indexed {ProcessedCount} of {TotalCount} items"
    )]
    private partial void LogBatchIndexed(int processedCount, int totalCount);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Search index rebuild completed: {ItemCount} items indexed in {ElapsedMs}ms"
    )]
    private partial void LogRebuildCompleted(int itemCount, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Search index rebuild was cancelled")]
    private partial void LogRebuildCancelled();

    [LoggerMessage(Level = LogLevel.Error, Message = "Search index rebuild failed")]
    private partial void LogRebuildFailed(Exception ex);
}
