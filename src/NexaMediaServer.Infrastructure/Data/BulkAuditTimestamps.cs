// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Provides helper methods to apply audit timestamps (CreatedAt, UpdatedAt)
/// to entities before bulk insert operations that bypass EF Core change tracking.
/// </summary>
/// <remarks>
/// EFCore.BulkExtensions bypasses the change tracker, so the <see cref="Interceptors.AuditTimestampsInterceptor"/>
/// does not run. This helper should be used to manually apply timestamps before calling BulkInsertAsync.
/// </remarks>
public static class BulkAuditTimestamps
{
    /// <summary>
    /// Applies <see cref="AuditableEntity.CreatedAt"/> and <see cref="AuditableEntity.UpdatedAt"/>
    /// timestamps to a collection of entities before bulk insert.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from <see cref="AuditableEntity"/>.</typeparam>
    /// <param name="entities">The collection of entities to apply timestamps to.</param>
    public static void ApplyInsertTimestamps<T>(IEnumerable<T> entities)
        where T : AuditableEntity
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = utcNow;
            entity.UpdatedAt = utcNow;
        }
    }

    /// <summary>
    /// Applies <see cref="AuditableEntity.UpdatedAt"/> timestamp to a collection of entities before bulk update.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from <see cref="AuditableEntity"/>.</typeparam>
    /// <param name="entities">The collection of entities to apply timestamps to.</param>
    public static void ApplyUpdateTimestamps<T>(IEnumerable<T> entities)
        where T : AuditableEntity
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.UpdatedAt = utcNow;
        }
    }
}
