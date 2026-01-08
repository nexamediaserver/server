// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Handles initialisation and seeding of the application's database, including roles, users, and default data.
/// </summary>
public partial class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> logger;
    private readonly UserManager<User> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly IServerSettingRepository serverSettingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContextInitialiser"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userManager">The user manager for application users.</param>
    /// <param name="roleManager">The role manager for identity roles.</param>
    /// <param name="serverSettingRepository">The server setting repository.</param>
    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IServerSettingRepository serverSettingRepository
    )
    {
        this.logger = logger;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.serverSettingRepository = serverSettingRepository;
    }

    /// <summary>
    /// Seeds the application's database with default roles, users, and data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await this.TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogDatabaseSeedingError(this.logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Attempts to seed the application's database with default roles, users, and data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task TrySeedAsync(CancellationToken cancellationToken = default)
    {
        // Ensure Administrator role exists
        if (!await this.roleManager.RoleExistsAsync(Roles.Administrator))
        {
            await this.roleManager.CreateAsync(new IdentityRole(Roles.Administrator));
        }

        // Create default admin user ONLY when there are no users yet.
        if (!this.userManager.Users.Any())
        {
            var administrator = new User
            {
                UserName = "admin",
                Email =
                    Environment.GetEnvironmentVariable("NEXA_ADMIN_EMAIL") ?? "admin@nexa.local",
                EmailConfirmed = true,
            };

            var createResult = await this.userManager.CreateAsync(
                administrator,
                Environment.GetEnvironmentVariable("NEXA_ADMIN_PASSWORD") ?? "changeme"
            );
            if (!createResult.Succeeded)
            {
                var error = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Failed to create default admin user: {error}"
                );
            }

            // Add to Administrator role
            await this.userManager.AddToRoleAsync(administrator, Roles.Administrator);
        }

        // Seed default server settings if they don't exist
        await this.SeedServerSettingsAsync(cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An error occurred while seeding the database."
    )]
    private static partial void LogDatabaseSeedingError(ILogger logger, Exception exception);

    /// <summary>
    /// Seeds default server settings if they don't exist in the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task SeedServerSettingsAsync(CancellationToken cancellationToken)
    {
        var settingsToSeed = new Dictionary<string, object>
        {
            { ServerSettingKeys.ServerName, ServerSettingDefaults.ServerName },
            { ServerSettingKeys.MaxStreamingBitrate, ServerSettingDefaults.MaxStreamingBitrate },
            { ServerSettingKeys.PreferH265, ServerSettingDefaults.PreferH265 },
            { ServerSettingKeys.AllowRemuxing, ServerSettingDefaults.AllowRemuxing },
            { ServerSettingKeys.AllowHEVCEncoding, ServerSettingDefaults.AllowHEVCEncoding },
            { ServerSettingKeys.DashVideoCodec, ServerSettingDefaults.DashVideoCodec },
            { ServerSettingKeys.DashAudioCodec, ServerSettingDefaults.DashAudioCodec },
            {
                ServerSettingKeys.DashSegmentDurationSeconds,
                ServerSettingDefaults.DashSegmentDurationSeconds
            },
            { ServerSettingKeys.EnableToneMapping, ServerSettingDefaults.EnableToneMapping },
            { ServerSettingKeys.AllowedTags, ServerSettingDefaults.AllowedTags },
            { ServerSettingKeys.BlockedTags, ServerSettingDefaults.BlockedTags },
            { ServerSettingKeys.GenreMappings, ServerSettingDefaults.GenreMappings },
            { ServerSettingKeys.LogLevel, ServerSettingDefaults.LogLevel },
        };

        foreach (var (key, defaultValue) in settingsToSeed)
        {
            var existing = await this.serverSettingRepository.GetByKeyAsync(key, cancellationToken);
            if (existing == null)
            {
                await this.serverSettingRepository.UpsertAsync(
                    key,
                    defaultValue.ToString() ?? string.Empty,
                    cancellationToken);
            }
        }
    }
}
