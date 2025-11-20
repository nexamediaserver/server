// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Provides the application directory paths for the Nexa Media Server.
/// </summary>
public partial class ApplicationPaths : IApplicationPaths
{
    private readonly ILogger<ApplicationPaths> logger;
    private readonly object initializationLock = new();
    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationPaths"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public ApplicationPaths(IConfiguration configuration, ILogger<ApplicationPaths> logger)
    {
        this.logger = logger;
        this.DataDirectory = DetermineDataDirectory(configuration);
        this.ConfigDirectory = Path.Combine(this.DataDirectory, "config");
        this.LogDirectory = Path.Combine(this.DataDirectory, "logs");
        this.CacheDirectory = Path.Combine(this.DataDirectory, "cache");
        this.MediaDirectory = Path.Combine(this.DataDirectory, "media");
        this.TempDirectory = Path.Combine(this.DataDirectory, "temp");
        this.DatabaseDirectory = Path.Combine(this.DataDirectory, "database");
        this.IndexDirectory = Path.Combine(this.DataDirectory, "index");
        this.BackupDirectory = Path.Combine(this.DataDirectory, "backups");
    }

    /// <inheritdoc/>
    public string DataDirectory { get; }

    /// <inheritdoc/>
    public string ConfigDirectory { get; }

    /// <inheritdoc/>
    public string LogDirectory { get; }

    /// <inheritdoc/>
    public string CacheDirectory { get; }

    /// <inheritdoc/>
    public string MediaDirectory { get; }

    /// <inheritdoc/>
    public string TempDirectory { get; }

    /// <inheritdoc/>
    public string DatabaseDirectory { get; }

    /// <inheritdoc/>
    public string IndexDirectory { get; }

    /// <inheritdoc/>
    public string BackupDirectory { get; }

    /// <summary>
    /// Creates an <see cref="ApplicationPaths"/> instance suitable for bootstrap scenarios where dependency injection and logging are not yet available.
    /// </summary>
    /// <param name="configuration">The configuration used to resolve directories.</param>
    /// <returns>An <see cref="ApplicationPaths"/> instance backed by a null logger.</returns>
    public static ApplicationPaths CreateForBootstrap(IConfiguration configuration)
    {
        var paths = new ApplicationPaths(configuration, NullLogger<ApplicationPaths>.Instance);
        paths.EnsureDirectoryExists(paths.LogDirectory);
        return paths;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (this.initialized)
        {
            return Task.CompletedTask;
        }

        lock (this.initializationLock)
        {
            if (this.initialized)
            {
                return Task.CompletedTask;
            }

            this.EnsureAllDirectoriesExist();
            this.LogPaths();

            this.initialized = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            this.LogDirectoryCreated(path);
            Directory.CreateDirectory(path);
        }
    }

    /// <inheritdoc/>
    public string GetDataPath(params string[] paths)
    {
        var combinedPaths = new[] { this.DataDirectory }.Concat(paths).ToArray();
        return Path.Combine(combinedPaths);
    }

    private static string DetermineDataDirectory(IConfiguration configuration)
    {
        // 1. Check environment variable (highest priority)
        var envDataDir = Environment.GetEnvironmentVariable("NEXA_DATA_DIR");
        if (!string.IsNullOrEmpty(envDataDir))
        {
            return Path.IsPathRooted(envDataDir)
                ? envDataDir
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), envDataDir));
        }

        // 2. Check configuration
        var configDataDir = configuration["Paths:Data"];
        if (!string.IsNullOrEmpty(configDataDir))
        {
            return Path.IsPathRooted(configDataDir)
                ? configDataDir
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configDataDir));
        }

        // 3. Use platform-specific defaults
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: %APPDATA%\NexaMediaServer
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NexaMediaServer"
            );
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Docker check - if running as root or UID 1000 in /app
            if (Environment.UserName == "root" || IsRunningInDocker())
            {
                // Docker: /config (single directory for simplicity)
                return "/config";
            }

            // Linux: Follow XDG Base Directory spec
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrEmpty(xdgDataHome))
            {
                return Path.Combine(xdgDataHome, "nexamediaserver");
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share",
                "nexamediaserver"
            );
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: ~/Library/Application Support/NexaMediaServer
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "NexaMediaServer"
            );
        }

        // Fallback: Use current directory
        return Path.Combine(Directory.GetCurrentDirectory(), "data");
    }

    private static bool IsRunningInDocker()
    {
        return File.Exists("/.dockerenv")
            || File.Exists("/run/.containerenv")
            || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating directory: {Path}")]
    private partial void LogDirectoryCreated(string path);

    private void EnsureAllDirectoriesExist()
    {
        this.EnsureDirectoryExists(this.DataDirectory);
        this.EnsureDirectoryExists(this.ConfigDirectory);
        this.EnsureDirectoryExists(this.LogDirectory);
        this.EnsureDirectoryExists(this.CacheDirectory);
        this.EnsureDirectoryExists(this.MediaDirectory);
        this.EnsureDirectoryExists(this.TempDirectory);
        this.EnsureDirectoryExists(this.DatabaseDirectory);
        this.EnsureDirectoryExists(this.IndexDirectory);
        this.EnsureDirectoryExists(this.BackupDirectory);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Directories: {@Paths}")]
    private partial void LogPaths(Dictionary<string, string> paths);

    private void LogPaths()
    {
        this.LogPaths(
            new Dictionary<string, string>
            {
                { "DataDirectory", this.DataDirectory },
                { "ConfigDirectory", this.ConfigDirectory },
                { "LogDirectory", this.LogDirectory },
                { "CacheDirectory", this.CacheDirectory },
                { "MediaDirectory", this.MediaDirectory },
                { "TempDirectory", this.TempDirectory },
                { "DatabaseDirectory", this.DatabaseDirectory },
                { "IndexDirectory", this.IndexDirectory },
                { "BackupDirectory", this.BackupDirectory },
            }
        );
    }
}
