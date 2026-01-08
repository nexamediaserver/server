// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload indicating the outcome of a library scan request.
/// </summary>
public sealed class StartLibraryScanPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartLibraryScanPayload"/> class.
    /// </summary>
    /// <param name="success">Whether the request succeeded.</param>
    /// <param name="scanId">The scan job identifier if successful.</param>
    /// <param name="error">Optional error message.</param>
    public StartLibraryScanPayload(bool success, int? scanId = null, string? error = null)
    {
        this.Success = success;
        this.ScanId = scanId;
        this.Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the scan job identifier.
    /// </summary>
    public int? ScanId { get; }

    /// <summary>
    /// Gets an optional error description for failed requests.
    /// </summary>
    public string? Error { get; }
}
