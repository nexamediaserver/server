// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;

using FFMpegCore;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg;

/// <summary>
/// Validates FFmpeg binary availability on server startup and blocks if not found.
/// </summary>
public sealed partial class FFmpegValidationService : IHostedService
{
    private readonly ILogger<FFmpegValidationService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegValidationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FFmpegValidationService(ILogger<FFmpegValidationService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogValidatingFFmpeg(this.logger);

        try
        {
            // Find FFmpeg binary in PATH
            var ffmpegPath = FindExecutableInPath("ffmpeg");

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                LogFFmpegNotFound(this.logger, "not found in PATH");
                throw new InvalidOperationException(
                    "FFmpeg binary not found in PATH. Please install FFmpeg to use Nexa Media Server. " +
                    "Visit https://ffmpeg.org/download.html for installation instructions.");
            }

            // Find FFprobe binary in PATH
            var ffprobePath = FindExecutableInPath("ffprobe");

            if (string.IsNullOrEmpty(ffprobePath))
            {
                LogFFprobeNotFound(this.logger, "not found in PATH");
                throw new InvalidOperationException(
                    "FFprobe binary not found in PATH. Please install FFmpeg (which includes FFprobe) to use Nexa Media Server. " +
                    "Visit https://ffmpeg.org/download.html for installation instructions.");
            }

            // Configure GlobalFFOptions with discovered paths
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegPath)! });

            LogFFmpegFound(this.logger, ffmpegPath, ffprobePath);
            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            LogFFmpegValidationFailed(this.logger, ex);
            throw new InvalidOperationException(
                "Failed to validate FFmpeg installation. Please ensure FFmpeg is installed and available in PATH.",
                ex);
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string? FindExecutableInPath(string executableName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        var pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';
        var paths = pathVariable.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var executableExtensions = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };

        foreach (var path in paths)
        {
            foreach (var extension in executableExtensions)
            {
                var fullPath = Path.Combine(path, executableName + extension);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Validating FFmpeg binary availability...")]
    private static partial void LogValidatingFFmpeg(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "FFmpeg binary not found at path: {Path}")]
    private static partial void LogFFmpegNotFound(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "FFprobe binary not found at path: {Path}")]
    private static partial void LogFFprobeNotFound(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "FFmpeg found at: {FFmpegPath}, FFprobe found at: {FFprobePath}")]
    private static partial void LogFFmpegFound(ILogger logger, string ffmpegPath, string ffprobePath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "FFmpeg validation failed")]
    private static partial void LogFFmpegValidationFailed(ILogger logger, Exception exception);
}
