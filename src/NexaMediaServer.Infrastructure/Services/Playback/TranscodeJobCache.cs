// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// In-memory cache for active transcode jobs with LRU eviction.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public sealed partial class TranscodeJobCache : ITranscodeJobCache, IDisposable
{
    private readonly ConcurrentDictionary<int, TranscodeJobCacheEntry> byId = new();
    private readonly ConcurrentDictionary<string, int> pathToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TranscodeJobCache> logger;
    private readonly Timer cleanupTimer;
    private readonly TimeSpan cleanupInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan defaultIdleTimeout = TimeSpan.FromMinutes(5);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeJobCache"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public TranscodeJobCache(ILogger<TranscodeJobCache> logger)
    {
        this.logger = logger;
        this.cleanupTimer = new Timer(
            this.CleanupCallback,
            null,
            this.cleanupInterval,
            this.cleanupInterval
        );
    }

    /// <inheritdoc />
    public void Register(int jobId, string outputPath, Process process, CancellationTokenSource cancellationTokenSource)
    {
        var entry = new TranscodeJobCacheEntry
        {
            JobId = jobId,
            OutputPath = outputPath,
            Process = process,
            CancellationTokenSource = cancellationTokenSource,
        };

        if (this.byId.TryAdd(jobId, entry))
        {
            this.pathToId[outputPath] = jobId;
            this.LogJobRegistered(jobId, outputPath);
        }
        else
        {
            this.LogJobAlreadyRegistered(jobId);
        }
    }

    /// <inheritdoc />
    public void Unregister(int jobId)
    {
        if (this.byId.TryRemove(jobId, out var entry))
        {
            this.pathToId.TryRemove(entry.OutputPath, out _);
            this.LogJobUnregistered(jobId, entry.OutputPath);
        }
    }

    /// <inheritdoc />
    public TranscodeJobCacheEntry? GetByPath(string outputPath)
    {
        if (this.pathToId.TryGetValue(outputPath, out var jobId))
        {
            return this.GetById(jobId);
        }

        return null;
    }

    /// <inheritdoc />
    public TranscodeJobCacheEntry? GetById(int jobId)
    {
        if (this.byId.TryGetValue(jobId, out var entry))
        {
            entry.LastAccessTime = DateTime.UtcNow;
            return entry;
        }

        return null;
    }

    /// <inheritdoc />
    public void Touch(int jobId)
    {
        if (this.byId.TryGetValue(jobId, out var entry))
        {
            entry.LastAccessTime = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public async Task<bool> KillByPathAsync(string outputPath, CancellationToken cancellationToken)
    {
        if (!this.pathToId.TryGetValue(outputPath, out var jobId))
        {
            return false;
        }

        return await this.KillByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> KillByIdAsync(int jobId, CancellationToken cancellationToken)
    {
        if (!this.byId.TryGetValue(jobId, out var entry))
        {
            return false;
        }

        this.LogKillingJob(jobId, entry.OutputPath);

        try
        {
            // Signal cancellation to the transcode task
            await entry.CancellationTokenSource.CancelAsync().ConfigureAwait(false);

            // Kill the FFmpeg process if still running
            if (!entry.Process.HasExited)
            {
                entry.Process.Kill(entireProcessTree: true);

                // Wait for process to exit with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    await entry.Process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    this.LogProcessKillTimeout(jobId);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        finally
        {
            this.Unregister(jobId);
        }

        this.LogJobKilled(jobId);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TranscodeJobCacheEntry> GetAll()
    {
        return this.byId.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public int EvictIdle(TimeSpan idleTimeout)
    {
        var cutoff = DateTime.UtcNow - idleTimeout;
        var evicted = 0;

        foreach (var kvp in this.byId)
        {
            if (kvp.Value.LastAccessTime < cutoff)
            {
                // Kill the process before evicting
                try
                {
                    if (!kvp.Value.Process.HasExited)
                    {
                        kvp.Value.CancellationTokenSource.Cancel();
                        kvp.Value.Process.Kill(entireProcessTree: true);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }

                this.Unregister(kvp.Key);
                evicted++;
                this.LogJobEvicted(kvp.Key, kvp.Value.OutputPath, idleTimeout);
            }
        }

        return evicted;
    }

    /// <inheritdoc />
    public int? GetCurrentTranscodingIndex(string outputPath)
    {
        var entry = this.GetByPath(outputPath);
        if (entry == null || entry.Process.HasExited)
        {
            return null;
        }

        try
        {
            if (!Directory.Exists(outputPath))
            {
                return null;
            }

            // DASH segments follow a naming pattern like: chunk-stream0-00001.m4s
            // We scan for the highest numbered segment file
            var segmentFiles = Directory.GetFiles(outputPath, $"*{entry.SegmentExtension}")
                .Where(f => !Path.GetFileName(f).StartsWith("init-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (segmentFiles.Count == 0)
            {
                return null;
            }

            int maxIndex = 0;
            foreach (var file in segmentFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Extract the segment number from filename (last numeric segment)
                var lastDash = fileName.LastIndexOf('-');
                if (lastDash >= 0 && lastDash < fileName.Length - 1)
                {
                    var numberPart = fileName[(lastDash + 1)..];
                    if (int.TryParse(numberPart, out var index) && index > maxIndex)
                    {
                        maxIndex = index;
                    }
                }
            }

            return maxIndex > 0 ? maxIndex : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.cleanupTimer.Dispose();

        // Kill all active transcodes on shutdown
        foreach (var entry in this.byId.Values)
        {
            try
            {
                if (!entry.Process.HasExited)
                {
                    entry.CancellationTokenSource.Cancel();
                    entry.Process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }

        this.byId.Clear();
        this.pathToId.Clear();
    }

    private void CleanupCallback(object? state)
    {
        try
        {
            var evicted = this.EvictIdle(this.defaultIdleTimeout);
            if (evicted > 0)
            {
                this.LogCleanupCompleted(evicted);
            }
        }
        catch (Exception ex)
        {
            this.LogCleanupError(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registered transcode job {JobId} for path {OutputPath}")]
    private partial void LogJobRegistered(int jobId, string outputPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transcode job {JobId} already registered in cache")]
    private partial void LogJobAlreadyRegistered(int jobId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unregistered transcode job {JobId} for path {OutputPath}")]
    private partial void LogJobUnregistered(int jobId, string outputPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Killing transcode job {JobId} for path {OutputPath}")]
    private partial void LogKillingJob(int jobId, string outputPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Timeout waiting for transcode job {JobId} process to exit")]
    private partial void LogProcessKillTimeout(int jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcode job {JobId} killed successfully")]
    private partial void LogJobKilled(int jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Evicted idle transcode job {JobId} for path {OutputPath} (idle > {IdleTimeout})")]
    private partial void LogJobEvicted(int jobId, string outputPath, TimeSpan idleTimeout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transcode job cache cleanup completed, evicted {Count} idle jobs")]
    private partial void LogCleanupCompleted(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during transcode job cache cleanup")]
    private partial void LogCleanupError(Exception ex);
}
