// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;


using Hangfire;

using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using NexaMediaServer.API.DataLoaders;
using NexaMediaServer.API.Services;
using NexaMediaServer.API.Services.Authentication;
using NexaMediaServer.API.Telemetry;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.JobFilters;
using NexaMediaServer.Infrastructure.Logging;
using NexaMediaServer.Infrastructure.Services;

using OpenTelemetry.Resources;

using Scalar.AspNetCore;

using Serilog;

const string rollingLogFileName = "server-.log";

var builder = WebApplication.CreateBuilder(args);

var bootstrapPaths = ApplicationPaths.CreateForBootstrap(builder.Configuration);
var fallbackLogDirectory = bootstrapPaths.LogDirectory;
var bootstrapLogPath = System.IO.Path.Combine(fallbackLogDirectory, rollingLogFileName);

Log.Logger = BootstrapLogger.Create(bootstrapLogPath);

Log.Information("Starting Nexa Media Server...");
Log.Information(
    "Environment: {Environment}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
);
Log.Information(
    "OS: {OS} {OSVersion} ({OSArchitecture})",
    RuntimeInformation.OSDescription,
    Environment.OSVersion.Version,
    RuntimeInformation.OSArchitecture
);
Log.Information("Framework: {Framework}", RuntimeInformation.FrameworkDescription);
Log.Information(
    "Process: {ProcessorCount} processors, {ProcessArchitecture} architecture",
    Environment.ProcessorCount,
    RuntimeInformation.ProcessArchitecture
);

var version = Assembly.GetExecutingAssembly().GetName().Version;
Log.Information("Application Version: {Version}", version?.ToString() ?? "Unknown");
Log.Information("Working Directory: {WorkingDirectory}", System.IO.Directory.GetCurrentDirectory());

builder.Services.AddSerilog(
    (services, configuration) =>
    {
        var logDirectory = bootstrapPaths.LogDirectory;
        var rollingLogPath = System.IO.Path.Combine(logDirectory, rollingLogFileName);

        configuration
            .MinimumLevel.ControlledBy(LoggingConfiguration.LevelSwitch)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.WithProperty("Application", "Nexa Media Server")
            .ReadFrom.Configuration(builder.Configuration);

        configuration.WriteTo.Console(
            outputTemplate: LoggingTemplates.DefaultOutputTemplate,
            formatProvider: CultureInfo.InvariantCulture
        );
        configuration.WriteTo.File(
            rollingLogPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true,
            outputTemplate: LoggingTemplates.DefaultOutputTemplate,
            formatProvider: CultureInfo.InvariantCulture
        );
    }
);

builder.Logging.AddOpenTelemetry(builder =>
{
    builder.IncludeFormattedMessage = true;
    builder.IncludeScopes = true;
    builder.ParseStateValues = true;
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NexaMediaServer"));
});

builder.AddTelemetry();

builder
    .Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.Lockout.AllowedForNewUsers = false;

        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MediaServerContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(
        IdentityConstants.ApplicationScheme,
        options =>
        {
            options.Cookie.Name = "nexa.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.SlidingExpiration = true;
            options.EventsType = typeof(SessionCookieAuthenticationEvents);
        }
    );

builder.Services.AddScoped<SessionCookieAuthenticationEvents>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.Administrator,
        policy => policy.RequireRole(Roles.Administrator)
    );
});

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    _ = options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Info.Title = "Nexa Media Server API";
            document.Info.Version =
                Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "v1";
            document.Info.Description = "API documentation for Nexa Media Server.";
#pragma warning disable S1075 // URIs should not be hardcoded
            document.Info.License = new OpenApiLicense
            {
                Name = "AGPL-3.0-or-later",
                Url = new Uri("https://www.gnu.org/licenses/agpl-3.0.en.html"),
            };
#pragma warning restore S1075 // URIs should not be hardcoded
            document.Info.Contact = new OpenApiContact
            {
                Name = "Nexa Contributors",
                Email = "contact@nexa.ms",
            };

            return Task.CompletedTask;
        }
    );
});

builder.AddInfrastructureServices();
builder.AddCoreServices();

builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddHttpContextAccessor();

// Metadata service now registered in Infrastructure layer (returns entities; API applies mapping).
builder
    .Services.AddGraphQLServer()
    .AddAuthorization()
    .RegisterDbContextFactory<MediaServerContext>()
    .AddTypes()
    .AddType<UploadType>()
    .AddDataLoader<ILibrarySectionByIdDataLoader, LibrarySectionByIdDataLoader>()
    .AddDataLoader<
        IRootMetadataItemsBySectionIdDataLoader,
        RootMetadataItemsBySectionIdDataLoader
    >()
    .AddDataLoader<
        IChildMetadataItemsByParentIdDataLoader,
        ChildMetadataItemsByParentIdDataLoader
    >()
    .AddDataLoader<IMetadataItemByIdDataLoader, MetadataItemByIdDataLoader>()
    .AddDataLoader<IMetadataItemSettingByUserDataLoader, MetadataItemSettingByUserDataLoader>()
    .AddGlobalObjectIdentification()
    .AddQueryFieldToMutationPayloads()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage()
    .AddInMemorySubscriptions()
    .AddInstrumentation()
    .ModifyRequestOptions(options =>
    {
        // Include exception details in responses during development
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
    });

// GraphQL notifier implementation for metadata item updates.
builder.Services.AddScoped<IMetadataItemUpdateNotifier, GraphQLMetadataItemUpdateNotifier>();

// GraphQL job notification publisher
builder.Services.AddScoped<IJobNotificationPublisher, GraphQLJobNotificationPublisher>();

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.EnableForHttps = true;
});

builder.Services.AddHangfire(
    (sp, config) =>
    {
        var filter = sp.GetRequiredService<JobStateNotificationFilter>();
        config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
            .UseFilter(filter);
    }
);

builder.Services.AddHangfireServer(options =>
{
    options.Queues =
    [
        "scans",
        "metadata_agents",
        "file_analyzers",
        "image_generators",
        "trickplay",
        "search_index",
    ];
});

var app = builder.Build();

try
{
    await app.ApplyMigrationsAsync("NEXA_SKIP_MIGRATIONS");
    await app.InitialiseDatabaseAsync();

    // Initialize log level from server settings
    using var scope = app.Services.CreateScope();
    var serverSettingService = scope.ServiceProvider.GetRequiredService<IServerSettingService>();
    var logLevel = await serverSettingService.GetAsync(
        ServerSettingKeys.LogLevel,
        ServerSettingDefaults.LogLevel,
        CancellationToken.None
    );
    LoggingConfiguration.TrySetMinimumLevel(logLevel);
    Log.Information("Log level set to: {LogLevel}", logLevel);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to initialize database. Server startup aborted.");
    Environment.Exit(1);
}

app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseRouting();
app.UseWebSockets();

app.UseStaticFiles();

if (app.Environment.IsProduction() && builder.Environment.WebRootPath is not null)
{
    var spaPath = System.IO.Path.Combine(builder.Environment.WebRootPath, "build");
    if (System.IO.Directory.Exists(spaPath))
    {
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(spaPath),
                RequestPath = "/web",
            }
        );
    }

    // SPA client-side routing under /web (if directory exists)
    if (System.IO.Directory.Exists(spaPath))
    {
        app.MapFallbackToFile("/web/{*path:nonfile}", "build/index.html");
    }
}

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapHangfireDashboard();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Schedule recurring job for purging old job notification history
var jobNotificationOptions = app
    .Services.GetRequiredService<IOptions<JobNotificationOptions>>()
    .Value;
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<IJobProgressReporter>(
    "purge-job-notification-history",
    reporter =>
        reporter.PurgeHistoryAsync(
            jobNotificationOptions.HistoryRetentionDays,
            CancellationToken.None
        ),
    Cron.Daily,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

recurringJobManager.AddOrUpdate<PlaybackCleanupJob>(
    "cleanup-stale-playback-sessions",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Weekly(DayOfWeek.Sunday, 3, 0),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapGraphQL().WithOptions(new GraphQLServerOptions { EnableBatching = true });

app.MapControllers();

await app.RunWithGraphQLCommandsAsync(args);

#pragma warning disable S1118 // Utility classes should not have public constructors
/// <summary>
/// Entry point for the Nexa Media Server application.
/// </summary>
public partial class Program { }
#pragma warning restore S1118 // Utility classes should not have public constructors
