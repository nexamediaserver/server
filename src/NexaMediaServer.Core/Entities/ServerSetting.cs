// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents a configurable setting for the server.
/// </summary>
public class ServerSetting : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique key that identifies the setting.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the stored value for the setting.
    /// </summary>
    public string Value { get; set; } = null!;
}
