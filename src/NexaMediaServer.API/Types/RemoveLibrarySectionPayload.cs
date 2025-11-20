// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL payload indicating the outcome of a library section removal request.
/// </summary>
public sealed class RemoveLibrarySectionPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveLibrarySectionPayload"/> class.
    /// </summary>
    /// <param name="success">Whether the request succeeded.</param>
    /// <param name="error">Optional error message.</param>
    public RemoveLibrarySectionPayload(bool success, string? error = null)
    {
        this.Success = success;
        this.Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets an optional error description for failed requests.
    /// </summary>
    public string? Error { get; }
}
