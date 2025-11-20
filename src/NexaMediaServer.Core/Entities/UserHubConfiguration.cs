// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents user-specific hub configuration for a specific context.
/// </summary>
public class UserHubConfiguration : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the user this configuration belongs to.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user this configuration belongs to.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the hub context this configuration applies to.
    /// </summary>
    public HubContext Context { get; set; }

    /// <summary>
    /// Gets or sets the optional library section ID for library-specific configurations.
    /// Null for Home context.
    /// </summary>
    public int? LibrarySectionId { get; set; }

    /// <summary>
    /// Gets or sets the library section this configuration applies to.
    /// </summary>
    public LibrarySection? LibrarySection { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled hub types in display order.
    /// Stored as JSON array of hub type enum values.
    /// </summary>
    public List<HubType> EnabledHubTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of explicitly disabled hub types.
    /// Stored as JSON array of hub type enum values.
    /// </summary>
    public List<HubType> DisabledHubTypes { get; set; } = [];
}
