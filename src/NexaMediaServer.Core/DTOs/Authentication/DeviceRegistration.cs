// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Represents the device metadata supplied during authentication.
/// </summary>
/// <param name="Identifier">Stable device identifier provided by the client.</param>
/// <param name="Name">Friendly display name chosen by the user/device.</param>
/// <param name="Platform">Operating system or device class string.</param>
/// <param name="Version">Optional application version.</param>
public sealed record class DeviceRegistration(
    string Identifier,
    string Name,
    string Platform,
    string? Version = null
);
