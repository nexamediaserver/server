// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Lightweight projection representing a persisted session for API responses.
/// </summary>
/// <param name="PublicId">Public GUID identifying the session.</param>
/// <param name="DeviceName">Friendly device name.</param>
/// <param name="Platform">Platform/OS string.</param>
/// <param name="ExpiresAt">UTC expiration timestamp.</param>
/// <param name="LastUsedAt">Optional UTC last-used timestamp.</param>
/// <param name="IsRevoked">Indicates whether the session has been revoked.</param>
public sealed record class SessionSummary(
    Guid PublicId,
    string DeviceName,
    string Platform,
    DateTime ExpiresAt,
    DateTime? LastUsedAt,
    bool IsRevoked
);
