// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Request payload required to create a new session for a user/device combination.
/// </summary>
/// <param name="UserId">Identity user id that owns the session.</param>
/// <param name="DeviceId">Device identifier that initiated the session.</param>
/// <param name="ClientVersion">Optional app version string reported during login.</param>
public sealed record class SessionCreationRequest(
    string UserId,
    int DeviceId,
    string? ClientVersion
);
