// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Hangfire;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Hosted service that initializes transcode job management on startup.
/// </summary>
public sealed partial class TranscodeJobStartupService : IHostedService
{
    private readonly ITranscodeJobManager transcodeJobManager;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly TranscodeOptions transcodeOptions;
    private readonly ILogger<TranscodeJobStartupService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeJobStartupService"/> class.
    /// </summary>
    /// <param name="transcodeJobManager">Transcode job manager.</param>
    /// <param name="recurringJobManager">Hangfire recurring job manager.</param>
    /// <param name="transcodeOptions">Transcode configuration options.</param>
    /// <param name="logger">Typed logger.</param>
    public TranscodeJobStartupService(
        ITranscodeJobManager transcodeJobManager,
        IRecurringJobManager recurringJobManager,
        IOptions<TranscodeOptions> transcodeOptions,
        ILogger<TranscodeJobStartupService> logger
    )
    {
        this.transcodeJobManager = transcodeJobManager;
        this.recurringJobManager = recurringJobManager;
        this.transcodeOptions = transcodeOptions.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.LogStarting();

        // Clean up any stale jobs from previous server run
        int staleJobsCleaned = await this.transcodeJobManager.CleanupStaleJobsAsync(
            cancellationToken
        );

        if (staleJobsCleaned > 0)
        {
            this.LogStaleJobsCleaned(staleJobsCleaned);
        }

        // Register the recurring segment cleanup job
        SegmentCleanupJob.Register(this.recurringJobManager);
        this.LogRecurringJobRegistered();

        // Register the idle job killer (runs every minute)
        this.recurringJobManager.AddOrUpdate<TranscodeJobStartupService>(
            "transcode-idle-killer",
            service => service.KillIdleJobsAsync(CancellationToken.None),
            Cron.Minutely()
        );

        this.LogStarted();
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.LogStopping();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Kills idle transcode jobs that haven't been pinged within the timeout.
    /// Called by Hangfire on a recurring schedule.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Queue("metadata_agents")]
    public async Task KillIdleJobsAsync(CancellationToken cancellationToken)
    {
        int killed = await this.transcodeJobManager.KillIdleJobsAsync(
            this.transcodeOptions.IdleTimeoutSeconds,
            cancellationToken
        );

        if (killed > 0)
        {
            this.LogIdleJobsKilled(killed);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcode job startup service starting")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcode job startup service started")]
    private partial void LogStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcode job startup service stopping")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaned up {Count} stale transcode jobs from previous server run")]
    private partial void LogStaleJobsCleaned(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registered recurring segment cleanup job")]
    private partial void LogRecurringJobRegistered();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Killed {Count} idle transcode jobs")]
    private partial void LogIdleJobsKilled(int count);
}
