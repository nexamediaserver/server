// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Base class for entities that support soft deletion.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was deleted, or null if not deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
