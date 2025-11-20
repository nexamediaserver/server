// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire;

/// <summary>
/// Throttles how many executions of the same job method may run concurrently across the whole Hangfire cluster.
/// Uses a bank of distributed locks (1..N) to allow up to N concurrent executions.
/// </summary>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Class,
    Inherited = true,
    AllowMultiple = false
)]
public class MaximumConcurrentExecutionsAttribute : JobFilterAttribute, IServerFilter
{
    // Local fast-path to skip trying locks already taken in this process.
    private static readonly ConcurrentDictionary<string, byte> LocalLocks = new();

    private readonly string slotLeaseItemKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumConcurrentExecutionsAttribute"/> class.
    /// </summary>
    /// <param name="maxConcurrentJobs">The number of concurrent executions allowed for the same job method.</param>
    /// <param name="timeoutInSeconds">Total time to wait to acquire a slot before failing.</param>
    /// <param name="pollingIntervalInSeconds">Delay between full lock scans when all slots are busy.</param>
    public MaximumConcurrentExecutionsAttribute(
        int maxConcurrentJobs,
        int timeoutInSeconds = 60,
        int pollingIntervalInSeconds = 3
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentJobs);
        ArgumentOutOfRangeException.ThrowIfNegative(timeoutInSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pollingIntervalInSeconds);

        this.MaxConcurrentJobs = maxConcurrentJobs;
        this.TimeoutInSeconds = timeoutInSeconds;
        this.PollingIntervalInSeconds = pollingIntervalInSeconds;
        this.slotLeaseItemKey = $"NexaMCE_SlotLease::{RuntimeHelpers.GetHashCode(this)}";
    }

    /// <summary>
    /// Gets the maximum number of concurrent executions allowed for the same job method.
    /// </summary>
    public int MaxConcurrentJobs { get; }

    /// <summary>
    /// Gets the delay, in seconds, between scans of all lock slots when they are all busy.
    /// </summary>
    public int PollingIntervalInSeconds { get; }

    /// <summary>
    /// Gets the total time, in seconds, to wait to acquire a slot before failing the job start.
    /// </summary>
    public int TimeoutInSeconds { get; }

    /// <summary>
    /// Called by Hangfire before the job method is executed. Attempts to acquire one of the N distributed locks.
    /// </summary>
    /// <param name="context">The performing context.</param>
    public void OnPerforming(PerformingContext context)
    {
        // Build a stable lock name per job method, including generic parameters.
        var baseName = BuildResourceBaseName(context.BackgroundJob.Job);
        var timeout = TimeSpan.FromSeconds(this.TimeoutInSeconds);
        var timeoutPerLock = TimeSpan.Zero; // immediate try per slot
        var started = Stopwatch.StartNew();
        var jobCancellation = context.CancellationToken;
        var shutdownToken = jobCancellation.ShutdownToken;

        do
        {
            jobCancellation.ThrowIfCancellationRequested();

            for (var i = 1; i <= this.MaxConcurrentJobs; i++)
            {
                var resourceName = $"{baseName}-{i}/{this.MaxConcurrentJobs}";

                if (!LocalLocks.TryAdd(resourceName, 0))
                {
                    continue;
                }

                IDisposable? handle = null;
                var acquired = false;

                try
                {
                    // Acquire distributed lock (immediate try, throws on timeout)
                    handle = context.Connection.AcquireDistributedLock(
                        resourceName,
                        timeoutPerLock
                    );

                    // Success: stash for release on OnPerformed
                    context.Items[this.slotLeaseItemKey] = new SlotLease(resourceName, handle);
                    acquired = true;
                }
                catch (DistributedLockTimeoutException)
                {
                    // Slot busy elsewhere: try the next slot
                }
                finally
                {
                    // Keep the local lock only when we successfully acquired the distributed handle
                    if (!acquired)
                    {
                        LocalLocks.TryRemove(resourceName, out _);
                    }
                }

                if (acquired)
                {
                    return;
                }
            }

            // All slots currently busy, wait and retry until total timeout
            if (this.PollingIntervalInSeconds > 0)
            {
                var delay = TimeSpan.FromSeconds(this.PollingIntervalInSeconds);
                if (shutdownToken.WaitHandle.WaitOne(delay))
                {
                    jobCancellation.ThrowIfCancellationRequested();
                }
            }
        } while (started.Elapsed < timeout);

        // Timed out without acquiring a slot
        throw new DistributedLockTimeoutException(baseName);
    }

    /// <summary>
    /// Called by Hangfire after the job completes. Releases the acquired distributed lock.
    /// </summary>
    /// <param name="context">The performed context.</param>
    public void OnPerformed(PerformedContext context)
    {
        if (!TryGetLease(context, this.slotLeaseItemKey, out var lease))
        {
            return;
        }

        try
        {
            lease.Handle.Dispose();
        }
        finally
        {
            LocalLocks.TryRemove(lease.Name, out _);
            context.Items.Remove(this.slotLeaseItemKey);
        }
    }

    private static string BuildResourceBaseName(Job job)
    {
        // Hangfire has internal TypeExtensions, so we produce a readable stable generic type name
        var typeName = ToGenericTypeString(job.Type);
        return $"{typeName}.{job.Method.Name}";
    }

    private static string ToGenericTypeString(Type type)
    {
        if (!type.IsGenericType)
        {
            return TrimNamespace(type);
        }

        var def = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments().Select(ToGenericTypeString);
        var typeName = TrimNamespace(def);
        var tickIndex = typeName.IndexOf('`');
        if (tickIndex >= 0)
        {
            typeName = typeName[..tickIndex];
        }

        return $"{typeName}<{string.Join(",", args)}>".Replace('+', '.');
    }

    private static string TrimNamespace(Type type)
    {
        var full = type.FullName ?? type.Name;
        if (!string.IsNullOrEmpty(type.Namespace))
        {
            var ns = type.Namespace + ".";
            if (full.StartsWith(ns, StringComparison.Ordinal))
            {
                full = full.Substring(ns.Length);
            }
        }

        return full.Replace('+', '.');
    }

    private static bool TryGetLease(
        PerformContext context,
        string slotLeaseItemKey,
        out SlotLease lease
    )
    {
        lease = default;

        if (
            context.Items.TryGetValue(slotLeaseItemKey, out var leaseObj)
            && leaseObj is SlotLease slotLease
        )
        {
            lease = slotLease;
            return true;
        }

        return false;
    }

    private readonly record struct SlotLease(string Name, IDisposable Handle);
}
