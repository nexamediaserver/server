// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Hubs;

/// <summary>
/// User configuration for hubs in a specific context.
/// </summary>
/// <param name="EnabledHubTypes">The list of enabled hub types in display order.</param>
/// <param name="DisabledHubTypes">The list of explicitly disabled hub types.</param>
public sealed record HubConfiguration(
    IReadOnlyList<HubType> EnabledHubTypes,
    IReadOnlyList<HubType> DisabledHubTypes
);
