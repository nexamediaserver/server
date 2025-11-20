// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration for session lifetime and JWT issuance.
/// </summary>
public sealed class SessionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Sessions";

    /// <summary>
    /// Gets or sets the default number of days before a session expires.
    /// </summary>
    public int LifetimeDays { get; set; } = 30;
}
