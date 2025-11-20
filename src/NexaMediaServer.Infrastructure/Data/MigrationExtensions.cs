// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Extension methods for applying migrations to the database.
/// </summary>
public static class MigrationExtensions
{
    // Compiled logging delegates (CA1848)
    private static readonly Action<ILogger, Exception?> LogSkipByEnv = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(2000, nameof(LogSkipByEnv)),
        "Skipping database migrations due to environment variable."
    );

    private static readonly Action<ILogger, string, Exception?> LogApplyingViaFactory =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2001, nameof(LogApplyingViaFactory)),
            "Applying migrations via IDbContextFactory for {DbContext}"
        );

    private static readonly Action<ILogger, string, Exception?> LogApplyingViaContext =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2002, nameof(LogApplyingViaContext)),
            "Applying migrations via resolved DbContext {DbContext}"
        );

    private static readonly Action<ILogger, Exception?> LogNoContextRegistered =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2003, nameof(LogNoContextRegistered)),
            "No MediaServerContext or IDbContextFactory<MediaServerContext> registered; skipping migration for that context"
        );

    /// <summary>
    /// Apply any pending migrations for the context to the database.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="skipMigrationsEnvVar">
    /// Optional environment variable name that, if set, will skip applying migrations.
    /// </param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task ApplyMigrationsAsync(
        this WebApplication app,
        string? skipMigrationsEnvVar = null
    )
    {
        var logger = app
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MigrationExtensions");

        if (
            skipMigrationsEnvVar is not null
            && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(skipMigrationsEnvVar))
        )
        {
            LogSkipByEnv(logger, null);
            return;
        }

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        // If app registered a DbContextFactory, prefer that for safety.
        // Should not be needed, but let's be cautious.
        var dbContextFactory = services.GetService<IDbContextFactory<MediaServerContext>>();
        if (dbContextFactory != null)
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();
            LogApplyingViaFactory(logger, nameof(MediaServerContext), null);
            await ctx.Database.MigrateAsync();
        }
        else
        {
            // If not, resolve the DbContext directly.
            var ctx = services.GetService<MediaServerContext>();
            if (ctx != null)
            {
                LogApplyingViaContext(logger, nameof(MediaServerContext), null);
                await ctx.Database.MigrateAsync();
            }
            else
            {
                LogNoContextRegistered(logger, null);
            }
        }
    }
}
