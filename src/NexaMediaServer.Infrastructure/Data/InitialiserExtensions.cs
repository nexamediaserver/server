// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Provides extension methods for initialising the application's database.
/// </summary>
public static class InitialiserExtensions
{
    /// <summary>
    /// Initialises and seeds the application's database on startup.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance to initialise the database for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.SeedAsync();
    }
}
