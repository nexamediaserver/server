// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs.Authentication;

/// <summary>
/// Represents the current state of a user's identity profile.
/// </summary>
/// <param name="Email">Primary email for the account.</param>
/// <param name="IsEmailConfirmed">Indicates whether the email address has been confirmed.</param>
public sealed record class InfoResponse(string Email, bool IsEmailConfirmed);
