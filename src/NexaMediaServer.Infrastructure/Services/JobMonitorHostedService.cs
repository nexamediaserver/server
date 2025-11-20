// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Hosted service that periodically monitors Hangfire jobs and publishes notifications.
/// </summary>
public sealed partial class JobMonitorHostedService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<JobMonitorHostedService> logger;
    private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="JobMonitorHostedService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public JobMonitorHostedService(
        IServiceProvider serviceProvider,
        ILogger<JobMonitorHostedService> logger
    )
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.LogJobMonitorStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this.serviceProvider.CreateScope();
                var jobNotificationService =
                    scope.ServiceProvider.GetRequiredService<IJobNotificationService>();

                var activeJobs = await jobNotificationService.GetActiveJobsAsync(stoppingToken);
                foreach (var job in activeJobs)
                {
                    await jobNotificationService.PublishNotificationAsync(job, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this.LogJobMonitorError(ex);
            }

            await Task.Delay(this.pollingInterval, stoppingToken);
        }

        this.LogJobMonitorStopped();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Job monitor started")]
    private partial void LogJobMonitorStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Job monitor stopped")]
    private partial void LogJobMonitorStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in job monitor")]
    private partial void LogJobMonitorError(Exception exception);
}
