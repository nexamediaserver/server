// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Interceptors;

/// <summary>
/// Intercepts delete operations to convert them into soft deletions.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is not null)
        {
            ApplySoftDelete(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
        {
            ApplySoftDelete(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySoftDelete(DbContext context)
    {
        IEnumerable<EntityEntry<SoftDeletableEntity>> entries = context
            .ChangeTracker.Entries<SoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<SoftDeletableEntity> softDeletable in entries)
        {
            softDeletable.State = EntityState.Modified;
            softDeletable.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
}
