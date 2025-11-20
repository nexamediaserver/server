// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents the outcome of enqueueing metadata refresh work.
/// </summary>
public sealed class MetadataRefreshResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataRefreshResult"/> class.
    /// </summary>
    /// <param name="success">Whether the enqueue operation succeeded.</param>
    /// <param name="error">Optional error message.</param>
    /// <param name="enqueued">How many jobs were enqueued.</param>
    private MetadataRefreshResult(bool success, string? error, int enqueued)
    {
        this.Success = success;
        this.Error = error;
        this.Enqueued = enqueued;
    }

    /// <summary>
    /// Gets a value indicating whether the enqueue operation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets an optional error description when the operation fails.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the number of jobs enqueued for processing.
    /// </summary>
    public int Enqueued { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="enqueued">Number of queued jobs.</param>
    /// <returns>A successful <see cref="MetadataRefreshResult"/> instance.</returns>
    public static MetadataRefreshResult SuccessResult(int enqueued) => new(true, null, enqueued);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">Error message describing the failure.</param>
    /// <returns>A failed <see cref="MetadataRefreshResult"/> instance.</returns>
    public static MetadataRefreshResult Failure(string error) => new(false, error, 0);
}
