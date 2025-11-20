// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Constants;

/// <summary>
/// Provides custom claim type constants used across the application.
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// Claim type used to embed the backing session identifier within auth cookies.
    /// </summary>
    public const string SessionId = "nexa:session_id";
}
