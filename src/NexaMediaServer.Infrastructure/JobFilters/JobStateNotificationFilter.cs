// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.JobFilters;

/// <summary>
/// Hangfire filter that captures job state transitions and notifies the <see cref="IJobProgressReporter"/>.
/// Automatically detects job type and library from job arguments.
/// </summary>
public sealed partial class JobStateNotificationFilter : IApplyStateFilter
{
    private static readonly HashSet<string> TrackedMethods =
    [
        "ExecuteScanAsync",
        "RefreshMetadataAsync",
        "AnalyzeFilesAsync",
        "GenerateImagesAsync",
        "GenerateTrickplayAsync",
    ];

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<JobStateNotificationFilter> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateNotificationFilter"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving scoped services.</param>
    /// <param name="logger">Logger instance.</param>
    public JobStateNotificationFilter(
        IServiceProvider serviceProvider,
        ILogger<JobStateNotificationFilter> logger
    )
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        var job = context.BackgroundJob.Job;
        if (job == null)
        {
            return;
        }

        var methodName = job.Method.Name;
        if (!TrackedMethods.Contains(methodName))
        {
            return;
        }

        var jobType = GetJobType(methodName);
        if (jobType == null)
        {
            return;
        }

        // Fire and forget - don't block the state transition
        _ = Task.Run(async () =>
        {
            try
            {
                await this.ProcessStateTransitionAsync(
                    context.BackgroundJob.Id,
                    job,
                    jobType.Value,
                    context.NewState
                );
            }
            catch (Exception ex)
            {
                this.LogStateTransitionError(context.BackgroundJob.Id, methodName, ex.Message);
            }
        });
    }

    /// <inheritdoc />
    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // Not needed - we track forward transitions only
    }

    private static JobType? GetJobType(string methodName) =>
        methodName switch
        {
            "ExecuteScanAsync" => JobType.LibraryScan,
            "RefreshMetadataAsync" => JobType.MetadataRefresh,
            "AnalyzeFilesAsync" => JobType.FileAnalysis,
            "GenerateImagesAsync" => JobType.ImageGeneration,
            "GenerateTrickplayAsync" => JobType.TrickplayGeneration,
            _ => null,
        };

    private static async Task<int?> ResolveScanLibraryAsync(
        MediaServerContext db,
        IReadOnlyList<object?> args
    )
    {
        // ExecuteScanAsync(int scanId)
        if (args.Count < 1 || args[0] is not int scanId)
        {
            return null;
        }

        return await db
            .LibraryScans.Where(s => s.Id == scanId)
            .Select(s => s.LibrarySectionId)
            .FirstOrDefaultAsync();
    }

    private static async Task<int?> ResolveMetadataItemLibraryAsync(
        MediaServerContext db,
        IReadOnlyList<object?> args
    )
    {
        // RefreshMetadataAsync(Guid uuid, ...), AnalyzeFilesAsync(Guid uuid), etc.
        if (args.Count < 1)
        {
            return null;
        }

        Guid? uuid = args[0] switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null,
        };

        if (uuid == null)
        {
            return null;
        }

        return await db
            .MetadataItems.Where(m => m.Uuid == uuid.Value)
            .Select(m => m.LibrarySectionId)
            .FirstOrDefaultAsync();
    }

    private async Task ProcessStateTransitionAsync(
        string jobId,
        Job job,
        JobType jobType,
        IState newState
    )
    {
        var methodName = job.Method.Name;
        var librarySectionId = await this.ResolveLibrarySectionIdAsync(job, jobType);

        if (librarySectionId == null)
        {
            this.LogCouldNotResolveLibrary(jobId, methodName);
            return;
        }

        var reporter = this.serviceProvider.GetRequiredService<IJobProgressReporter>();

        switch (newState.Name)
        {
            case "Enqueued":
                // Job enqueued - start tracking (we'll update with real totals when job reports)
                await reporter.StartAsync(librarySectionId.Value, jobType, 0);
                this.LogJobEnqueued(jobId, methodName, librarySectionId.Value);
                break;

            case "Processing":
                // Job started processing
                await reporter.StartAsync(librarySectionId.Value, jobType, 0);
                this.LogJobProcessing(jobId, methodName, librarySectionId.Value);
                break;

            case "Succeeded":
                // Job completed successfully
                await reporter.CompleteAsync(librarySectionId.Value, jobType);
                this.LogJobSucceeded(jobId, methodName, librarySectionId.Value);
                break;

            case "Failed":
                var failedState = (FailedState)newState;
                var errorMessage = failedState.Exception?.Message ?? "Unknown error";
                await reporter.FailAsync(librarySectionId.Value, jobType, errorMessage);
                this.LogJobFailed(jobId, methodName, librarySectionId.Value, errorMessage);
                break;

            case "Deleted":
                // Job was cancelled/deleted
                await reporter.FailAsync(librarySectionId.Value, jobType, "Job was cancelled");
                this.LogJobDeleted(jobId, methodName, librarySectionId.Value);
                break;
        }
    }

    private async Task<int?> ResolveLibrarySectionIdAsync(Job job, JobType jobType)
    {
        if (job.Args == null || job.Args.Count == 0)
        {
            return null;
        }

        using var scope = this.serviceProvider.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<
            IDbContextFactory<MediaServerContext>
        >();
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return jobType switch
        {
            JobType.LibraryScan => await ResolveScanLibraryAsync(db, job.Args),
            _ => await ResolveMetadataItemLibraryAsync(db, job.Args),
        };
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Job {JobId} ({MethodName}) enqueued for library {LibrarySectionId}"
    )]
    private partial void LogJobEnqueued(string jobId, string methodName, int librarySectionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Job {JobId} ({MethodName}) processing for library {LibrarySectionId}"
    )]
    private partial void LogJobProcessing(string jobId, string methodName, int librarySectionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Job {JobId} ({MethodName}) succeeded for library {LibrarySectionId}"
    )]
    private partial void LogJobSucceeded(string jobId, string methodName, int librarySectionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Job {JobId} ({MethodName}) failed for library {LibrarySectionId}: {ErrorMessage}"
    )]
    private partial void LogJobFailed(
        string jobId,
        string methodName,
        int librarySectionId,
        string errorMessage
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Job {JobId} ({MethodName}) deleted for library {LibrarySectionId}"
    )]
    private partial void LogJobDeleted(string jobId, string methodName, int librarySectionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not resolve library for job {JobId} ({MethodName})"
    )]
    private partial void LogCouldNotResolveLibrary(string jobId, string methodName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error processing state transition for job {JobId} ({MethodName}): {ErrorMessage}"
    )]
    private partial void LogStateTransitionError(
        string jobId,
        string methodName,
        string errorMessage
    );
}
