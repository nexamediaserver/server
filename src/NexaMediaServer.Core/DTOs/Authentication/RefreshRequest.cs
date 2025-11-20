// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Represents the payload for refresh requests. Maintained for backward compatibility with prior JWT flows.
/// </summary>
public sealed record class RefreshRequest
{
    /// <summary>
    /// Gets the optional refresh token. Value is ignored for cookie-authenticated sessions.
    /// </summary>
    [MaxLength(512)]
    public string? RefreshToken { get; init; }
}
