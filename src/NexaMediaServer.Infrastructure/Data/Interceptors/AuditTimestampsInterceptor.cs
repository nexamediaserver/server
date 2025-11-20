// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that manages creation and update timestamps for auditable entities.
/// </summary>
public class AuditTimestampsInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        DateTime utcNow = DateTime.UtcNow;
        IEnumerable<EntityEntry<AuditableEntity>> auditableEntries = eventData
            .Context.ChangeTracker.Entries<AuditableEntity>()
            .Where(entry =>
                entry.State is EntityState.Added
                || entry.State is EntityState.Modified
                || entry.State is EntityState.Deleted
            );

        foreach (EntityEntry<AuditableEntity> entry in auditableEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(AuditableEntity.CreatedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    break;

                case EntityState.Deleted:
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.UpdatedAt)).IsModified = false;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
